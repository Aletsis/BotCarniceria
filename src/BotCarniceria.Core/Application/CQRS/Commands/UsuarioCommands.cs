using MediatR;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public record CreateUsuarioCommand(string Username, string Password, string Nombre, RolUsuario Rol, string? Telefono) : IRequest<bool>;
public record UpdateUsuarioCommand(int UsuarioID, string Nombre, RolUsuario Rol, string? Telefono) : IRequest<bool>;
public record ToggleUsuarioActivoCommand(int UsuarioID, bool Activo) : IRequest<bool>;
public record ResetUserLockoutCommand(int UsuarioID) : IRequest<bool>;
public record ChangePasswordCommand(int UsuarioID, string NewPassword) : IRequest<bool>;
