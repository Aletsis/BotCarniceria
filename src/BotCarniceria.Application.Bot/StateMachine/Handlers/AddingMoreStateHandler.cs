using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class AddingMoreStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;

    public AddingMoreStateHandler(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, Conversacion session)
    {
        // Agregar los nuevos productos al pedido existente
        var pedidoActual = session.Buffer ?? "";
        var pedidoActualizado = $"{pedidoActual}\n{messageContent}";
        
        // Mostrar resumen actualizado
        var resumen = $"📋 *Resumen actualizado de tu pedido:*\n\n{pedidoActualizado}\n\n¿Qué deseas hacer?";
        
        var buttons = new List<(string id, string title)>
        {
            ("order_confirm", "✅ Confirmar pedido"),
            ("order_add_more", "➕ Agregar más")
        };

        await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, resumen, buttons);
        
        // Volver al estado de confirmación con el pedido actualizado
        session.GuardarBuffer(pedidoActualizado);
        session.CambiarEstado(ConversationState.AWAITING_CONFIRM);
    }
}
