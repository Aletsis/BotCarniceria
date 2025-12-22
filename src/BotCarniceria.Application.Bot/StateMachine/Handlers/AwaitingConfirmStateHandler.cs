using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class AwaitingConfirmStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;

    public AwaitingConfirmStateHandler(
        IWhatsAppService whatsAppService,
        IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session)
    {
        if (messageContent == "order_confirm")
        {
            // Cliente confirma el pedido, ahora pedir confirmación de dirección
            var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
            if (cliente == null) return; // Should not happen if flow is correct, or handle gracefully

            var direccionActual = cliente.Direccion ?? "No registrada";
            
            var mensaje = $"📍 *Confirmación de Dirección*\n\n" +
                         $"Dirección registrada:\n*{direccionActual}*\n\n" +
                         $"¿Es correcta esta dirección de entrega?";
            
            var buttons = new List<(string id, string title)>
            {
                ("address_correct", "✅ Sí, es correcta"),
                ("address_wrong", "📝 No, cambiar")
            };

            await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, mensaje, buttons);
            
            // Cambiar al estado de confirmación de dirección
            session.CambiarEstado(ConversationState.CONFIRM_ADDRESS);
        }
        else if (messageContent == "order_add_more")
        {
            // Cliente quiere agregar más productos
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "➕ Perfecto! ¿Qué más deseas agregar a tu pedido?");
            
            // Cambiar al estado de agregar más, manteniendo el pedido actual en el buffer
            session.CambiarEstado(ConversationState.ADDING_MORE);
        }
    }
}
