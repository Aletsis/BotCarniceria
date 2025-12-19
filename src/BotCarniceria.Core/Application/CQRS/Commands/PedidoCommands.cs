using BotCarniceria.Core.Application.DTOs;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public class CreatePedidoCommand : IRequest<PedidoDto>
{
    public int ClienteID { get; set; }
    public string Contenido { get; set; } = string.Empty;
    public string? Notas { get; set; }
    public string? FormaPago { get; set; }
}

public class UpdatePedidoEstadoCommand : IRequest<bool>
{
    public long PedidoID { get; set; }
    public string NuevoEstado { get; set; } = string.Empty;
}

public class CancelPedidoCommand : IRequest<bool>
{
    public long PedidoID { get; set; }
    public string? Motivo { get; set; }
}

public class ImprimirPedidoCommand : IRequest<bool>
{
    public long PedidoID { get; set; }
}

public class CreateOrUpdateClienteCommand : IRequest<ClienteDto>
{
    public string NumeroTelefono { get; set; } = string.Empty;
    public string? Nombre { get; set; }
    public string? Direccion { get; set; }
}
