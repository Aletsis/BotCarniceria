using MediatR;

namespace BotCarniceria.Core.Domain.Events;

public interface IDomainEvent : INotification
{
    DateTime OccurredOn { get; }
}
