using BotCarniceria.Core.Application.Events;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Core.Application.EventHandlers;

public class PrintJobFailedEventHandler : INotificationHandler<PrintJobFailedEvent>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<PrintJobFailedEventHandler> _logger;

    public PrintJobFailedEventHandler(
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        ILogger<PrintJobFailedEventHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task Handle(PrintJobFailedEvent notification, CancellationToken cancellationToken)
    {
        try
        {
            var pedido = await _unitOfWork.Orders.GetByIdAsync(notification.PedidoId);
            if (pedido == null)
            {
                _logger.LogWarning("Pedido {PedidoId} not found when handling print failure event", notification.PedidoId);
                return;
            }

            // Specification to find Admins and Supervisors
            var spec = new AdminAndSupervisorUsersSpecification();
            var admins = await _unitOfWork.Users.FindAsync(spec);

            if (!admins.Any())
            {
                _logger.LogWarning("No admins or supervisors found to notify about print failure for Pedido {PedidoId}", notification.PedidoId);
                return;
            }

            var attemptText = notification.AttemptNumber == 1
                ? "en el primer intento"
                : $"en el intento #{notification.AttemptNumber}";

            var actionText = notification.IsFinal
                ? "⛔ Se han agotado los reintentos. Revise la impresora MANUALMENTE."
                : "🔄 El sistema reintentará automáticamente.";

            foreach (var admin in admins)
            {
                if (!string.IsNullOrEmpty(admin.Telefono))
                {
                    var message = $"🚨 *ALERTA DE IMPRESIÓN*\n\n" +
                                  $"❌ Error al imprimir el ticket del pedido *{pedido.Folio.Value}* {attemptText}.\n" +
                                  $"{actionText}\n\n" +
                                  $"📋 Cliente: {pedido.Cliente?.Nombre ?? "N/A"}\n" +
                                  $"📞 Teléfono: {pedido.Cliente?.NumeroTelefono ?? "N/A"}\n\n" +
                                  $"⚠️ Por favor verifique el estado de la impresora.";

                    // SendTextMessageAsync handles persistence to the Messages table internally.
                    var sent = await _whatsAppService.SendTextMessageAsync(admin.Telefono, message);
                    
                    if (!sent)
                    {
                        _logger.LogWarning("Failed to send print failure notification to {Phone} for Pedido {PedidoId}", admin.Telefono, notification.PedidoId);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling PrintJobFailedEvent for Pedido {PedidoId}", notification.PedidoId);
        }
    }
}
