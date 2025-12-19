using BotCarniceria.Core.Domain.Entities;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class OrdersByClienteIdSpecification : Specification<Pedido>
{
    private readonly int _clienteId;

    public OrdersByClienteIdSpecification(int clienteId)
    {
        _clienteId = clienteId;
    }

    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return p => p.ClienteID == _clienteId;
    }
}
