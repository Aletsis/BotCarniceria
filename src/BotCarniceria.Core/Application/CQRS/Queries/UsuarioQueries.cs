using MediatR;
using BotCarniceria.Core.Application.DTOs;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public record GetAllUsuariosQuery : IRequest<List<UsuarioDto>>;
public record GetUsuarioByIdQuery(int Id) : IRequest<UsuarioDto?>;
public record LoginUserQuery(string Username, string Password) : IRequest<UsuarioDto?>;
