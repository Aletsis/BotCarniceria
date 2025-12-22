using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class ConfirmAddressStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmAddressStateHandler(
        IWhatsAppService whatsAppService,
        IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session)
    {
        if (messageContent == "address_correct")
        {
            // La dirección es correcta, preguntar forma de pago
            var mensaje = "💳 *Forma de Pago*\n\n¿Cómo deseas pagar tu pedido?";
            
            var buttons = new List<(string id, string title)>
            {
                ("payment_cash", "💵 Efectivo"),
                ("payment_card", "💳 Tarjeta")
            };

            await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, mensaje, buttons);
            
            session.CambiarEstado(ConversationState.SELECT_PAYMENT);
        }
        else if (messageContent == "address_wrong")
        {
            // El cliente quiere cambiar la dirección
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "📍 Por favor, escribe tu nueva dirección de entrega:");
            
            // Cambiar al estado ASK_ADDRESS pero mantener el pedido en el buffer
            session.CambiarEstado(ConversationState.ASK_ADDRESS);
        }
        else
        {
            // Respuesta no reconocida, volver a preguntar
            var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
            if (cliente == null) return;

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
        }
    }
}
