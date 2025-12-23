using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Constants;
using Hangfire;
using MediatR;
using BotCarniceria.Core.Application.Events;

namespace BotCarniceria.Infrastructure.BackgroundJobs.Handlers;

/// <summary>
/// Handler para trabajos de impresión de tickets
/// </summary>
public class PrintJobHandler : IJobHandler<EnqueuePrintJob>
{
    private readonly IPrintingService _printingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PrintJobHandler> _logger;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IPublisher _publisher;

    public PrintJobHandler(
        IPrintingService printingService,
        IUnitOfWork unitOfWork,
        ILogger<PrintJobHandler> logger,
        IBackgroundJobClient backgroundJobClient,
        IPublisher publisher)
    {
        _printingService = printingService;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
        _publisher = publisher;
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
                await _publisher.Publish(new PrintJobFailedEvent(job.PedidoId, nextRetryCount, false), cancellationToken);

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
                 await _publisher.Publish(new PrintJobFailedEvent(job.PedidoId, job.RetryCount + 1, true), cancellationToken); // Final failure
            }

            // We do NOT throw here, merging the catch block logic to stop Hangfire automatic retries (since we handle it manually)
        }
    }
}
