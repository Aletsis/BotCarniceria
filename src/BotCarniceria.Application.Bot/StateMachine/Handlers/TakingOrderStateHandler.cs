using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class TakingOrderStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public TakingOrderStateHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session)
    {
        if (messageType != TipoContenidoMensaje.Texto)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "❌ Por favor, escribe tu pedido en texto.");
            return;
        }

        // Mostrar resumen del pedido con opciones
        var resumen = $"📋 *Resumen de tu pedido:*\n\n{messageContent}\n\n¿Qué deseas hacer?";
        
        var buttons = new List<(string id, string title)>
        {
            ("order_confirm", "✅ Confirmar pedido"),
            ("order_add_more", "➕ Agregar más")
        };

        await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, resumen, buttons);
        
        // Guardar el pedido en el buffer y cambiar al estado de confirmación
        session.GuardarBuffer(messageContent);
        session.CambiarEstado(ConversationState.AWAITING_CONFIRM);
    }
}
