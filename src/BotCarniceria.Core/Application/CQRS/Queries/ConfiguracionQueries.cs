using MediatR;
using BotCarniceria.Core.Application.DTOs;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public record GetAllConfiguracionesQuery : IRequest<List<ConfiguracionDto>>;
public record GetConfiguracionByKeyQuery(string Key) : IRequest<ConfiguracionDto?>;
