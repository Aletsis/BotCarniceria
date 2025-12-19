using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Constants;
using Hangfire;

namespace BotCarniceria.Infrastructure.BackgroundJobs.Handlers;

/// <summary>
/// Handler para trabajos de impresión de tickets
/// </summary>
public class PrintJobHandler : IJobHandler<EnqueuePrintJob>
{
    private readonly IPrintingService _printingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<PrintJobHandler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;

    public PrintJobHandler(
        IPrintingService printingService,
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        ILogger<PrintJobHandler> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _printingService = printingService;
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    /// <summary>
    /// Ejecuta el trabajo de impresión con reintentos configurables
    /// </summary>
    [AutomaticRetry(Attempts = 0)] // Disable automatic retries to use dynamic configuration
    public async Task ExecuteAsync(EnqueuePrintJob job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Executing Print job {JobId} for Pedido {PedidoId} (Attempt {RetryCount})",
                job.JobId,
                job.PedidoId,
                job.RetryCount + 1);

            // Obtener el pedido
            var pedido = await _unitOfWork.Orders.GetByIdAsync(job.PedidoId);
            if (pedido == null)
            {
                _logger.LogWarning("Pedido {PedidoId} not found for print job {JobId}", job.PedidoId, job.JobId);
                return; // No reintentar si el pedido no existe
            }

            // Obtener cliente
            var cliente = await _unitOfWork.Clientes.GetByIdAsync(pedido.ClienteID);
            if (cliente == null)
            {
                _logger.LogWarning("Cliente not found for Pedido {PedidoId}", job.PedidoId);
                return;
            }

            // Construir contenido del pedido
            var contenido = pedido.Contenido ?? "Sin detalles";
            var notas = pedido.Notas ?? string.Empty;

            // Imprimir ticket
            var success = await _printingService.PrintTicketAsync(
                folio: pedido.Folio.Value,
                nombre: cliente.Nombre,
                telefono: cliente.NumeroTelefono,
                direccion: cliente.Direccion ?? "N/A",
                contenido: contenido,
                notas: notas);

            if (!success)
            {
                throw new InvalidOperationException(
                    $"Failed to print ticket for Pedido {job.PedidoId}");
            }

            // Imprimir duplicado si se solicita
            if (job.PrintDuplicate)
            {
                _logger.LogInformation("Printing duplicate for Pedido {PedidoId}", job.PedidoId);
                await _printingService.PrintTicketAsync(
                    folio: $"{pedido.Folio.Value} (COPIA)",
                    nombre: cliente.Nombre,
                    telefono: cliente.NumeroTelefono,
                    direccion: cliente.Direccion ?? "N/A",
                    contenido: contenido,
                    notas: notas);
            }

            _logger.LogInformation(
                "Print job {JobId} completed successfully for Pedido {PedidoId}",
                job.JobId,
                job.PedidoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Print job {JobId} failed for Pedido {PedidoId}.",
                job.JobId,
                job.PedidoId);

            // Load retry configuration
            var maxRetriesStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.System.PrintRetryCount) ?? "3";
            var retryIntervalStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.System.RetryIntervalSeconds) ?? "60";
            
            int.TryParse(maxRetriesStr, out int maxRetries);
            int.TryParse(retryIntervalStr, out int retryInterval);

            if (job.RetryCount < maxRetries)
            {
                var nextRetryCount = job.RetryCount + 1;
                _logger.LogWarning("Scheduling retry {NextRetry} for job {JobId} in {Interval} seconds", nextRetryCount, job.JobId, retryInterval);
                
                // Notify admin about failure (optional per attempt)
                await NotifyAdminsOfPrintFailureAsync(job.PedidoId, nextRetryCount);

                // Schedule next attempt
                _backgroundJobClient.Schedule<PrintJobHandler>(
                     service => service.ExecuteAsync(
                         new EnqueuePrintJob 
                         { 
                             JobId = job.JobId,
                             PedidoId = job.PedidoId,
                             PrinterName = job.PrinterName,
                             PrintDuplicate = job.PrintDuplicate,
                             RetryCount = nextRetryCount,
                             MaxRetries = job.MaxRetries, // Keep original legacy if needed
                             Priority = job.Priority
                         }, 
                         CancellationToken.None),
                     TimeSpan.FromSeconds(retryInterval));
            }
            else
            {
                 _logger.LogError("Max retries ({MaxRetries}) exceeded for job {JobId}. Giving up.", maxRetries, job.JobId);
                 await NotifyAdminsOfPrintFailureAsync(job.PedidoId, job.RetryCount + 1, true); // Final failure
            }

            // We do NOT throw here, merging the catch block logic to stop Hangfire automatic retries (since we handle it manually)
        }
    }

    /// <summary>
    /// Notifica a los administradores cuando falla la impresión
    /// </summary>
    private async Task NotifyAdminsOfPrintFailureAsync(long pedidoId, int attemptNumber, bool isFinal = false)
    {
        try
        {
            var pedido = await _unitOfWork.Orders.GetByIdAsync(pedidoId);
            if (pedido == null) return;

            var spec = new AdminAndSupervisorUsersSpecification();
            var admins = await _unitOfWork.Users.FindAsync(spec);
            
            var attemptText = attemptNumber == 1 
                ? "en el primer intento" 
                : $"en el intento #{attemptNumber}";

            var actionText = isFinal
                ? "⛔ Se han agotado los reintentos. Revise la impresora MANUALMENTE."
                : "🔄 El sistema reintentará automáticamente.";

            foreach (var admin in admins)
            {
                if (!string.IsNullOrEmpty(admin.Telefono))
                {
                    await _whatsAppService.SendTextMessageAsync(admin.Telefono,
                        $"🚨 *ALERTA DE IMPRESIÓN*\n\n" +
                        $"❌ Error al imprimir el ticket del pedido *{pedido.Folio.Value}* {attemptText}.\n" +
                        $"{actionText}\n\n" +
                        $"📋 Cliente: {pedido.Cliente?.Nombre ?? "N/A"}\n" +
                        $"📞 Teléfono: {pedido.Cliente?.NumeroTelefono ?? "N/A"}\n\n" +
                        $"⚠️ Por favor verifique el estado de la impresora.");
                }
            }

            _logger.LogWarning(
                "Notified {AdminCount} administrators about print failure for Pedido {PedidoId} (Attempt {AttemptNumber}, Final: {IsFinal})",
                admins.Count(),
                pedidoId,
                attemptNumber,
                isFinal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify admins about print failure for Pedido {PedidoId}", pedidoId);
        }
    }
}
