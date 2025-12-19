using BotCarniceria.Core.Domain.Models;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public record UpdatePrinterSettingsCommand(PrinterSettings Settings) : IRequest<bool>;
