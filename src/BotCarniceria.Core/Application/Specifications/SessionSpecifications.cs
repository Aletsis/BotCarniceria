using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class ExpiringSessionsSpecification : Specification<Conversacion>
{
    private readonly int _warningMinutesBefore;
    private readonly int? _overrideTimeoutMinutes;

    public ExpiringSessionsSpecification(int warningMinutesBeforeExpiration = 2, int? overrideTimeoutMinutes = null)
    {
        _warningMinutesBefore = warningMinutesBeforeExpiration;
        _overrideTimeoutMinutes = overrideTimeoutMinutes;
    }

    public override Expression<Func<Conversacion, bool>> ToExpression()
    {
        // Use override if present, else entity property
        // Note: EF Core translation of 'local variable' inside expression works fine.
        return c => c.Estado != ConversationState.START &&
                    !c.NotificacionTimeoutEnviada &&
                    DateTime.UtcNow >= c.UltimaActividad.AddMinutes((_overrideTimeoutMinutes ?? c.TimeoutEnMinutos) - _warningMinutesBefore) &&
                    DateTime.UtcNow < c.UltimaActividad.AddMinutes(_overrideTimeoutMinutes ?? c.TimeoutEnMinutos);
    }
}

public class ExpiredSessionsSpecification : Specification<Conversacion>
{
    private readonly int? _overrideTimeoutMinutes;

    public ExpiredSessionsSpecification(int? overrideTimeoutMinutes = null)
    {
        _overrideTimeoutMinutes = overrideTimeoutMinutes;
    }

    public override Expression<Func<Conversacion, bool>> ToExpression()
    {
        return c => c.Estado != ConversationState.START &&
                    DateTime.UtcNow >= c.UltimaActividad.AddMinutes(_overrideTimeoutMinutes ?? c.TimeoutEnMinutos);
    }
}

public class Expiring24hSessionsSpecification : Specification<Conversacion>
{
    private readonly int _hoursThreshold;

    public Expiring24hSessionsSpecification(int hoursThreshold = 23)
    {
        _hoursThreshold = hoursThreshold;
    }

    public override Expression<Func<Conversacion, bool>> ToExpression()
    {
        // Check if 23 hours (default) have passed since UltimaActividad, but less than 24 hours.
        // And notification not sent.
        // We only care if we can still send messages ( < 24h ).
        return c => !c.Notificacion24hEnviada &&
                    DateTime.UtcNow >= c.UltimaActividad.AddHours(_hoursThreshold) &&
                    DateTime.UtcNow < c.UltimaActividad.AddHours(24);
    }
}
