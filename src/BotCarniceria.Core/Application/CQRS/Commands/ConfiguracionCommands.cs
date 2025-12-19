using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public record UpdateConfiguracionCommand(string Key, string Value) : IRequest<bool>;
