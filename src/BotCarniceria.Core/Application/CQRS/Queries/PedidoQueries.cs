using BotCarniceria.Core.Application.DTOs;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public class GetPedidoByIdQuery : IRequest<PedidoDto?>
{
    public long PedidoID { get; set; }
}

public class GetPedidoByFolioQuery : IRequest<PedidoDto?>
{
    public string Folio { get; set; } = string.Empty;
}

public class GetAllPedidosQuery : IRequest<List<PedidoDto>>
{
}

public class GetPedidosByClienteQuery : IRequest<List<PedidoDto>>
{
    public int ClienteID { get; set; }
}

public class GetPendingPedidosQuery : IRequest<List<PedidoDto>>
{
}

public class GetTodayPedidosQuery : IRequest<List<PedidoDto>>
{
}

public class GetActiveClientePedidosQuery : IRequest<List<PedidoDto>>
{
    public int ClienteID { get; set; }
}

public class SearchPedidosQuery : IRequest<List<PedidoDto>>
{
    public string SearchTerm { get; set; } = string.Empty;
}

public class GetPedidosByDateRangeQuery : IRequest<List<PedidoDto>>
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class GetFilteredPedidosQuery : IRequest<List<PedidoDto>>
{
    public string? SearchTerm { get; set; }
    public string? Estado { get; set; }
    public DateTime? Date { get; set; }
    public bool OnlyToday { get; set; }
}
