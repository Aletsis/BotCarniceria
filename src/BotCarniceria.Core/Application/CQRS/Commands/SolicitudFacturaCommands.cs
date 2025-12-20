using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public class CreateSolicitudFacturaCommand : IRequest<long>
{
    public int ClienteID { get; set; }
    public string Folio { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string UsoCFDI { get; set; } = string.Empty;
    public string? Notas { get; set; }
}

public class UpdateSolicitudFacturaEstadoCommand : IRequest<bool>
{
    public long SolicitudFacturaID { get; set; }
    public string NuevoEstado { get; set; } = string.Empty;
    public string? Notas { get; set; }
}
