using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

/// <summary>
/// Handler for getting an order by ID
/// </summary>
public class GetPedidoByIdQueryHandler : IRequestHandler<GetPedidoByIdQuery, PedidoDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPedidoByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PedidoDto?> Handle(GetPedidoByIdQuery request, CancellationToken cancellationToken)
    {
        var pedido = await _unitOfWork.Orders.GetByIdAsync(request.PedidoID);
        return pedido == null ? null : PedidoDto.FromEntity(pedido);
    }
}

/// <summary>
/// Handler for getting an order by folio
/// </summary>
public class GetPedidoByFolioQueryHandler : IRequestHandler<GetPedidoByFolioQuery, PedidoDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPedidoByFolioQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PedidoDto?> Handle(GetPedidoByFolioQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosByFolioSpecification(request.Folio);
        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        var pedido = pedidos.FirstOrDefault();
        return pedido == null ? null : PedidoDto.FromEntity(pedido);
    }
}

/// <summary>
/// Handler for getting all orders
/// </summary>
public class GetAllPedidosQueryHandler : IRequestHandler<GetAllPedidosQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllPedidosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetAllPedidosQuery request, CancellationToken cancellationToken)
    {
        var pedidos = await _unitOfWork.Orders.GetAllAsync();
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for getting orders by customer
/// </summary>
public class GetPedidosByClienteQueryHandler : IRequestHandler<GetPedidosByClienteQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPedidosByClienteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetPedidosByClienteQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosByClienteSpecification(request.ClienteID);
        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for getting pending orders
/// </summary>
public class GetPendingPedidosQueryHandler : IRequestHandler<GetPendingPedidosQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPendingPedidosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetPendingPedidosQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosPendingSpecification();
        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for getting today's orders
/// </summary>
public class GetTodayPedidosQueryHandler : IRequestHandler<GetTodayPedidosQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTodayPedidosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetTodayPedidosQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosTodaySpecification();
        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for getting active orders for a customer
/// </summary>
public class GetActiveClientePedidosQueryHandler : IRequestHandler<GetActiveClientePedidosQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetActiveClientePedidosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetActiveClientePedidosQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosByClienteSpecification(request.ClienteID)
            .And(new PedidosActiveSpecification());

        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for searching orders by term
/// </summary>
public class SearchPedidosQueryHandler : IRequestHandler<SearchPedidosQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public SearchPedidosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(SearchPedidosQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosBySearchTermSpecification(request.SearchTerm);
        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for getting orders by date range
/// </summary>
public class GetPedidosByDateRangeQueryHandler : IRequestHandler<GetPedidosByDateRangeQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPedidosByDateRangeQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetPedidosByDateRangeQuery request, CancellationToken cancellationToken)
    {
        var spec = new PedidosByDateRangeSpecification(request.StartDate, request.EndDate);
        var pedidos = await _unitOfWork.Orders.FindAsync(spec);
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }
}

/// <summary>
/// Handler for getting filtered orders with multiple criteria
/// </summary>
public class GetFilteredPedidosQueryHandler : IRequestHandler<GetFilteredPedidosQuery, List<PedidoDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetFilteredPedidosQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<PedidoDto>> Handle(GetFilteredPedidosQuery request, CancellationToken cancellationToken)
    {
        var spec = BuildSpecification(request);

        // Get orders
        List<Pedido> pedidos;
        if (spec == null)
        {
            pedidos = await _unitOfWork.Orders.GetAllAsync();
        }
        else
        {
            pedidos = await _unitOfWork.Orders.FindAsync(spec);
        }

        // Apply ordering (descending by date)
        return pedidos.OrderByDescending(p => p.Fecha).Select(PedidoDto.FromEntity).ToList();
    }

    private Specification<Pedido>? BuildSpecification(GetFilteredPedidosQuery request)
    {
        Specification<Pedido>? spec = null;

        // Filter by today if requested
        if (request.OnlyToday)
        {
            spec = new PedidosTodaySpecification();
        }

        // Filter by specific date
        if (request.Date.HasValue)
        {
            var startDate = request.Date.Value.Date;
            var endDate = startDate.AddDays(1).AddTicks(-1);
            var dateSpec = new PedidosByDateRangeSpecification(startDate, endDate);
            spec = spec == null ? dateSpec : spec.And(dateSpec);
        }

        // Filter by status
        if (!string.IsNullOrEmpty(request.Estado) && request.Estado != "Todos")
        {
            var estadoSpec = GetEstadoSpecification(request.Estado);
            if (estadoSpec != null)
            {
                spec = spec == null ? estadoSpec : spec.And(estadoSpec);
            }
        }

        // Filter by search term
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchSpec = new PedidosBySearchTermSpecification(request.SearchTerm);
            spec = spec == null ? searchSpec : spec.And(searchSpec);
        }

        return spec;
    }

    private Specification<Pedido>? GetEstadoSpecification(string estado)
    {
        if (Enum.TryParse<EstadoPedido>(estado, out var estadoEnum))
        {
            return new PedidosByEstadoSpecification(estadoEnum);
        }

        // Handle legacy string mappings
        EstadoPedido? mappedState = estado switch
        {
            "En espera de surtir" => EstadoPedido.EnEspera,
            "En espera" => EstadoPedido.EnEspera,
            "En ruta" => EstadoPedido.EnRuta,
            "Entregado" => EstadoPedido.Entregado,
            "Cancelado" => EstadoPedido.Cancelado,
            _ => null
        };

        return mappedState.HasValue ? new PedidosByEstadoSpecification(mappedState.Value) : null;
    }
}
