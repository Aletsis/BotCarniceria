using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using System.Linq.Expressions;

namespace BotCarniceria.Core.Application.Specifications;

public class PedidosByClienteSpecification : Specification<Pedido>
{
    private readonly int _clienteId;

    public PedidosByClienteSpecification(int clienteId)
    {
        _clienteId = clienteId;
    }

    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return pedido => pedido.ClienteID == _clienteId;
    }
}

public class PedidosByEstadoSpecification : Specification<Pedido>
{
    private readonly EstadoPedido _estado;

    public PedidosByEstadoSpecification(EstadoPedido estado)
    {
        _estado = estado;
    }

    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return pedido => pedido.Estado == _estado;
    }
}

public class PedidosByDateRangeSpecification : Specification<Pedido>
{
    private readonly DateTime _startDate;
    private readonly DateTime _endDate;

    public PedidosByDateRangeSpecification(DateTime startDate, DateTime endDate)
    {
        _startDate = startDate;
        _endDate = endDate;
    }

    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return pedido => pedido.Fecha >= _startDate && pedido.Fecha <= _endDate;
    }
}

public class PedidosByFolioSpecification : Specification<Pedido>
{
    private readonly string _folio;

    public PedidosByFolioSpecification(string folio)
    {
        _folio = folio;
    }

    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        // Asumiendo que Value Object Folio se mapea adecuadamente o accedemos a Value si es Owned
        return pedido => pedido.Folio.Value == _folio;
    }
}

public class PedidosTodaySpecification : Specification<Pedido>
{
    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        var today = DateTime.UtcNow.Date; // Use UTC
        var tomorrow = today.AddDays(1);
        return pedido => pedido.Fecha >= today && pedido.Fecha < tomorrow;
    }
}

public class PedidosPendingSpecification : Specification<Pedido>
{
    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return pedido => pedido.Estado == EstadoPedido.EnEspera;
    }
}

public class PedidosActiveSpecification : Specification<Pedido>
{
    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return pedido => pedido.Estado != EstadoPedido.Cancelado 
                      && pedido.Estado != EstadoPedido.Entregado;
    }
}

public class PedidosBySearchTermSpecification : Specification<Pedido>
{
    private readonly string _searchTerm;

    public PedidosBySearchTermSpecification(string searchTerm)
    {
        _searchTerm = searchTerm?.ToLower() ?? throw new ArgumentNullException(nameof(searchTerm));
    }

    public override Expression<Func<Pedido, bool>> ToExpression()
    {
        return pedido => pedido.Folio.Value.ToLower().Contains(_searchTerm)
                      || pedido.Contenido.ToLower().Contains(_searchTerm);
    }
}
