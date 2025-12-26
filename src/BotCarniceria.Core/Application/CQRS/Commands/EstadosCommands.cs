using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public record UploadEstadoCommand(Stream FileStream, string FileName, string ContentType, string? Caption) : IRequest<string?>;
