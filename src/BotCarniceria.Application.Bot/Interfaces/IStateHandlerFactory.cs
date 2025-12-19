using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.Interfaces;

public interface IStateHandlerFactory
{
    IConversationStateHandler GetHandler(ConversationState state);
}
