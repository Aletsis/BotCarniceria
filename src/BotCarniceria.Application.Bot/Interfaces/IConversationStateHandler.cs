using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.Interfaces;

public interface IConversationStateHandler
{
    Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session);
}
