using BotCarniceria.Core.Domain.Entities;

namespace BotCarniceria.Core.Domain.Events;

public class PedidoCreatedEvent : IDomainEvent
{
    public Pedido Pedido { get; }
    public DateTime OccurredOn { get; }

    public PedidoCreatedEvent(Pedido pedido)
    {
        Pedido = pedido;
        OccurredOn = DateTime.UtcNow;
    }
}
