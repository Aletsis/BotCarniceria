using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums; // Ensure this contains START, MENU, ASK_NAME, etc.

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class AskNameStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AskNameStateHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session)
    {
        if (messageType != TipoContenidoMensaje.Texto)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "❌ Respuesta no válida. Por favor, escribe tu nombre en texto.");
            return;
        }

        session.GuardarNombreTemporal(messageContent);
        session.CambiarEstado(ConversationState.ASK_ADDRESS);

        await _whatsAppService.SendTextMessageAsync(phoneNumber, $"Gracias {messageContent}! 😊\n\n📍 Ahora, por favor indícame tu dirección de entrega:");
    }
}
