using BotCarniceria.Core.Application.DTOs.WhatsApp;

namespace BotCarniceria.Core.Application.Interfaces;

public interface IIncomingMessageHandler
{
    Task HandleAsync(WhatsAppMessage message);
}
