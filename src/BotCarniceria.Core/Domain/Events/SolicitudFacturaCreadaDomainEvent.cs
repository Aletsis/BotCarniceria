using BotCarniceria.Core.Domain.Entities;
using MediatR;

namespace BotCarniceria.Core.Domain.Events;

public class SolicitudFacturaCreadaDomainEvent : IDomainEvent, INotification
{
    public SolicitudFactura Solicitud { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public SolicitudFacturaCreadaDomainEvent(SolicitudFactura solicitud)
    {
        Solicitud = solicitud;
    }
}
