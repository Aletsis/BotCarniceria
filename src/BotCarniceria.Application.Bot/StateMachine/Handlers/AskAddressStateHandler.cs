using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects; // Maybe?

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class AskAddressStateHandler : IConversationStateHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;

    public AskAddressStateHandler(
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService)
    {
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session)
    {
        if (messageType != TipoContenidoMensaje.Texto)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "❌ Respuesta no válida. Por favor, escribe tu dirección en texto.");
            return;
        }

        try
        {
            // Begin transaction for atomic operation
            await _unitOfWork.BeginTransactionAsync();

            var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
            
            if (cliente == null)
            {
                cliente = Cliente.Create(phoneNumber, session.NombreTemporal ?? "Sin nombre", messageContent);
                await _unitOfWork.Clientes.AddAsync(cliente);
            }
            else
            {
                if (!string.IsNullOrEmpty(session.NombreTemporal))
                    cliente.UpdateNombre(session.NombreTemporal);
                cliente.UpdateDireccion(messageContent);
                await _unitOfWork.Clientes.UpdateAsync(cliente);
            }

            // Verificar si hay un pedido en el buffer (viene de cambiar dirección durante confirmación)
            if (!string.IsNullOrEmpty(session.Buffer))
            {
                // Commit transaction before updating session persistence (if handled by caller, we just commit DB changes here)
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Update session state
                session.CambiarEstado(ConversationState.SELECT_PAYMENT);

                // Hay un pedido pendiente, confirmar dirección actualizada y pedir forma de pago
                var mensaje = $"✅ Dirección actualizada correctamente.\n\n" +
                             $"📍 Nueva dirección:\n*{messageContent}*\n\n" +
                             $"💳 *Forma de Pago*\n\n¿Cómo deseas pagar tu pedido?";
                
                var buttons = new List<(string id, string title)>
                {
                    ("payment_cash", "💵 Efectivo"),
                    ("payment_card", "💳 Tarjeta")
                };

                await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, mensaje, buttons);
            }
            else
            {
                // Commit transaction
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                // Update session state
                session.CambiarEstado(ConversationState.TAKING_ORDER);

                // Flujo normal: nuevo pedido
                await _whatsAppService.SendTextMessageAsync(phoneNumber, 
                    $"Perfecto! 📝\n\nAhora puedes escribir tu pedido.\nIncluye cantidades y especificaciones.\n\nEjemplo:\n2 kg de carne molida\n1 kg de bistec\n500g de chorizo");
            }
        }
        catch
        {
            // Rollback transaction on error
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
