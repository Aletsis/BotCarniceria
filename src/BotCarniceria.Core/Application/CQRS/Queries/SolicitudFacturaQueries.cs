using BotCarniceria.Core.Application.DTOs;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public class GetAllSolicitudesFacturaQuery : IRequest<List<SolicitudFacturaDto>>
{
}

public class GetSolicitudFacturaByIdQuery : IRequest<SolicitudFacturaDto?>
{
    public long SolicitudFacturaID { get; set; }
}

public class GetSolicitudesFacturaByClienteQuery : IRequest<List<SolicitudFacturaDto>>
{
    public int ClienteID { get; set; }
}
