using BotCarniceria.Core.Application.DTOs.WhatsApp;

namespace BotCarniceria.Application.Bot.Interfaces;

public interface IIncomingMessageHandler
{
    Task HandleAsync(WhatsAppMessage message);
}
