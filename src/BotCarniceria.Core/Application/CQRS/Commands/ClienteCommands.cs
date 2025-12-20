using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public record UpdateClienteCommand(int ClienteID, string Nombre, string? Direccion) : IRequest<bool>;
public record ToggleClienteActivoCommand(int ClienteID, bool Activo) : IRequest<bool>;

public class CreateClienteCommand : IRequest<int>
{
    public string NumeroTelefono { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
}

public class UpdateClienteDatosFacturacionCommand : IRequest<bool>
{
    public int ClienteID { get; set; }
    public string RazonSocial { get; set; } = string.Empty;
    public string RFC { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Colonia { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RegimenFiscal { get; set; } = string.Empty;
}

