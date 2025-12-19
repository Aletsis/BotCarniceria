using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class ConfirmLateOrderStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmLateOrderStateHandler(
        IWhatsAppService whatsAppService,
        IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, Conversacion session)
    {
        if (messageContent == "late_order_continue")
        {
            // Proceed with order
            await InitOrderProcessAsync(phoneNumber, session);
        }
        else if (messageContent == "late_order_cancel")
        {
            // Cancel and show menu
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Entendido, operación cancelada.");

            var greeting = "Por favor, selecciona una opción del menú:";
            var buttons = new List<(string id, string title)>
            {
                ("menu_hacer_pedido", "🛒 Hacer pedido"),
                ("menu_estado_pedido", "📦 Estado de pedido"),
                ("menu_informacion", "ℹ️ Información")
            };
            
            await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, greeting, buttons);
            session.CambiarEstado(ConversationState.MENU);
        }
        else
        {
            // Invalid input, reiterate
             var warningMessage = "⚠️ *Aviso de Horario*\n\n" +
                                 "Los pedidos son únicamente hasta las 4:00 P.M.\n" +
                                 "Sin embargo, podemos tomar tu pedido para *surtirlo y entregarlo al día siguiente*.\n\n" +
                                 "¿Deseas continuar?";

            var buttons = new List<(string id, string title)>
            {
                ("late_order_continue", "✅ Continuar"),
                ("late_order_cancel", "❌ Cancelar")
            };

            await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, warningMessage, buttons);
        }
    }

    private async Task InitOrderProcessAsync(string phoneNumber, Conversacion session)
    {
        var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
        
        // Should not be null at this point normally, but safety check
        if (cliente == null)
        {
             cliente = Cliente.Create(phoneNumber, "Nuevo Cliente", "Sin Dirección");
             await _unitOfWork.Clientes.AddAsync(cliente);
             await _unitOfWork.SaveChangesAsync();
        }
        
        if (string.IsNullOrEmpty(cliente.Nombre) || cliente.Nombre == "Nuevo Cliente") // Check "Nuevo Cliente" or empty
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Para hacer un pedido, primero necesito algunos datos.\n\n📝 Por favor, indícame tu nombre completo:");
            session.CambiarEstado(ConversationState.ASK_NAME);
        }
        else if (string.IsNullOrEmpty(cliente.Direccion) || cliente.Direccion == "Sin Dirección")
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "📍 Por favor, indícame tu dirección de entrega:");
            session.CambiarEstado(ConversationState.ASK_ADDRESS);
        }
        else
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, $"Perfecto {cliente.Nombre}! 📝\n\nPor favor, escribe tu pedido.\nPuedes incluir cantidades y especificaciones.\n\nEjemplo:\n2 kg de carne molida\n1 kg de bistec\n500g de chorizo");
            session.CambiarEstado(ConversationState.TAKING_ORDER);
        }
    }
}
