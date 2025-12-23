using MediatR;

namespace BotCarniceria.Core.Application.Events;

public class PrintJobFailedEvent : INotification
{
    public long PedidoId { get; }
    public int AttemptNumber { get; }
    public bool IsFinal { get; }

    public PrintJobFailedEvent(long pedidoId, int attemptNumber, bool isFinal)
    {
        PedidoId = pedidoId;
        AttemptNumber = attemptNumber;
        IsFinal = isFinal;
    }
}
