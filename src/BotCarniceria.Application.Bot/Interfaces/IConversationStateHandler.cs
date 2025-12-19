using BotCarniceria.Core.Domain.Entities;

namespace BotCarniceria.Application.Bot.Interfaces;

public interface IConversationStateHandler
{
    Task HandleAsync(string phoneNumber, string messageContent, Conversacion session);
}
