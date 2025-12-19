using BotCarniceria.Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Application.Bot.EventHandlers;

/// <summary>
/// Handler for PedidoCreatedEvent - Example of domain event handling
/// </summary>
public class PedidoCreatedEventHandler : INotificationHandler<PedidoCreatedEvent>
{
    private readonly ILogger<PedidoCreatedEventHandler> _logger;

    public PedidoCreatedEventHandler(ILogger<PedidoCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PedidoCreatedEvent notification, CancellationToken cancellationToken)
    {
        // Log the event
        _logger.LogInformation(
            "Pedido creado: {Folio} para cliente {ClienteID} - Contenido: {Contenido}",
            notification.Pedido.Folio.Value,
            notification.Pedido.ClienteID,
            notification.Pedido.Contenido);

        // Here you could:
        // - Send notifications
        // - Update read models
        // - Trigger other business processes
        // - Publish to message bus

        return Task.CompletedTask;
    }
}
