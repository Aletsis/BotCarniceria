using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Constants;


namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class MenuStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;
    public MenuStateHandler(
        IWhatsAppService whatsAppService,
        IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, Conversacion session)
    {
        if (messageContent == "menu_hacer_pedido")
        {
            var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);

            // Check configured warning hour (default 16:00)
            var warningHourStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Orders.LateOrderWarningStartHour);
            int warningHour = 16;
            if (!string.IsNullOrEmpty(warningHourStr) && int.TryParse(warningHourStr, out int parsedHour))
            {
                warningHour = parsedHour;
            }

            if (DateTime.Now.Hour >= warningHour)
            {
                var timeString = DateTime.Today.AddHours(warningHour).ToString("h:mm tt");
                var warningMessage = "⚠️ *Aviso de Horario*\n\n" +
                                     $"Los pedidos son únicamente hasta las {timeString}.\n" +
                                     "Sin embargo, podemos tomar tu pedido para *surtirlo y entregarlo al día siguiente*.\n\n" +
                                     "¿Deseas continuar?";

                var buttons = new List<(string id, string title)>
                {
                    ("late_order_continue", "✅ Continuar"),
                    ("late_order_cancel", "❌ Cancelar")
                };

                await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, warningMessage, buttons);
                session.CambiarEstado(ConversationState.CONFIRM_LATE_ORDER);
                return;
            }

            await InitOrderProcessAsync(phoneNumber, session, cliente);
        }

        else if (messageContent == "menu_estado_pedido")
        {
            var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
            
            if (cliente == null)
            {
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "No tienes pedidos registrados aún. 🛒\n\n¿Te gustaría hacer uno?");
                return;
            }

            var spec = new OrdersByClienteIdSpecification(cliente.ClienteID);
            var pedidos = await _unitOfWork.Orders.FindAsync(spec);
            var recentPedidos = pedidos.OrderByDescending(p => p.Fecha).Take(3).ToList();

            if (!recentPedidos.Any())
            {
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "No tienes pedidos registrados aún. 🛒\n\n¿Te gustaría hacer uno?");
            }
            else
            {
                var mensaje = "📦 *Tus últimos pedidos:*\n\n";
                foreach (var pedido in recentPedidos)
                {
                    mensaje += $"*Folio:* {pedido.Folio}\n";
                    mensaje += $"*Estado:* {pedido.Estado}\n";
                    mensaje += $"*Fecha:* {pedido.Fecha:dd/MM/yyyy}\n";
                    mensaje += "-------------------\n";
                }
                await _whatsAppService.SendTextMessageAsync(phoneNumber, mensaje);

                var greeting = "Necesitas que te ayude con algo mas?";
                await ShowMainMenuAsync(phoneNumber, greeting);
            }
            }
        else if (messageContent == "menu_solicitar_factura")
        {
            var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
            if (cliente == null)
            {
                // Should not happen if they are seeing the menu, but safe guard.
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "Primero necesito registrar tus datos básicos. 📝\n\n¿Cual es tu nombre completo?");
                session.CambiarEstado(ConversationState.ASK_NAME);
                return;
            }

            // Show warning about daily billing
            var warningMessage = "⚠️ *Aviso Importante*\n\n" +
                                 "Nuestra facturación es diaria, en caso de que tu ticket de compra sea de algún día pasado no se podrá generar tu factura.\n\n" +
                                 "¿Deseas continuar?";

            var buttons = new List<(string id, string title)>
            {
                ("billing_warning_continue", "✅ Continuar"),
                ("billing_warning_cancel", "❌ Cancelar")
            };

            await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, warningMessage, buttons);
            session.CambiarEstado(ConversationState.BILLING_WARNING);
        }
        else if (messageContent == "menu_informacion")
        {
            // Default values
            string horariosDefault = "Lun-Sáb 8:00 AM - 8:00 PM\nDom 8:00 AM - 2:00 PM";
            string direccionDefault = "No disponible";
            string telefonoDefault = "No disponible";
            string tiempoEntregaDefault = "60-90 minutos";

            // Fetch from DB
            var horarios = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Business.Schedule) ?? horariosDefault;
            var direccion = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Business.Address) ?? direccionDefault;
            var telefono = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Business.Phone) ?? telefonoDefault;
            var tiempoEntrega = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Business.DeliveryTime) ?? tiempoEntregaDefault;
            
            var mensaje = "ℹ️ *Información de la Carnicería*\n\n";
            mensaje += $"📍 *Dirección:*\n{direccion}\n\n";
            mensaje += $"📞 *Teléfono:*\n{telefono}\n\n";
            mensaje += $"🕐 *Horarios:*\n{horarios}\n\n";
            mensaje += $"🚚 *Entregas a domicilio*\nTiempo estimado: {tiempoEntrega}\n\n";
            mensaje += "¿Necesitas algo más?";
            
            await ShowMainMenuAsync(phoneNumber, mensaje);
        }
        else
        {
            var greeting = "Por favor, selecciona una opción del menú:";
            await ShowMainMenuAsync(phoneNumber, greeting);
        }
    }

    private async Task ShowMainMenuAsync(string phoneNumber, string message)
    {
        string title = "Bienvenido";
        var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);

        string greeting;
        if (cliente != null && !string.IsNullOrEmpty(cliente.Nombre))
        {
            greeting = $"¡Hola {cliente.Nombre}! 👋 Es un gusto tenerte de vuelta.";
        }
        else
        {
            greeting = "¡Hola! 👋 Bienvenido/a a Carnicería La Blanquita. \nSoy Blanqui un bot diseñado para ayudarte a: \n Hacer pedidos\n Consultar el estado de tus pedidos \n Obtener información sobre nuestra sucursal.";
        }

        string message2 = "¿En qué puedo ayudarte hoy?";

        var rows = new List<(string id, string title, string? description)>
        {
            ("menu_hacer_pedido", "🛒 Hacer pedido", "Realiza un nuevo pedido"),
            ("menu_estado_pedido", "📦 Estado de pedido", "Consulta tus pedidos recientes"),
            ("menu_solicitar_factura", "🧾 Solicitar factura", "Factura tu compra"),
            ("menu_informacion", "ℹ️ Información", "Horarios y ubicación")
        };

        await _whatsAppService.SendInteractiveListAsync(phoneNumber, greeting, "Ver Menú", rows, title, message2);
    }

    private async Task InitOrderProcessAsync(string phoneNumber, Conversacion session, Cliente? cliente = null)
    {
        if (cliente == null)
        {
            cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
        }
        
        if (cliente == null || string.IsNullOrEmpty(cliente.Nombre))
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Para hacer un pedido, primero necesito algunos datos.\n\n📝 Por favor, indícame tu nombre completo:");
            session.CambiarEstado(ConversationState.ASK_NAME);
        }
        else if (string.IsNullOrEmpty(cliente.Direccion))
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
