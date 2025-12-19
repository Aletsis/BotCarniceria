using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public record UpdateClienteCommand(int ClienteID, string Nombre, string? Direccion) : IRequest<bool>;
public record ToggleClienteActivoCommand(int ClienteID, bool Activo) : IRequest<bool>;
