using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class ActiveSessionsSpecification : Specification<Conversacion>
{
    public override Expression<Func<Conversacion, bool>> ToExpression()
    {
        return c => c.Estado != ConversationState.START;
    }
}
