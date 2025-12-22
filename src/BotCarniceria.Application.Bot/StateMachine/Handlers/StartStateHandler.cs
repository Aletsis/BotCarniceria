using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class StartStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StartStateHandler> _logger;

    public StartStateHandler(
        IWhatsAppService whatsAppService,
        IUnitOfWork unitOfWork,
        ILogger<StartStateHandler> logger)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, TipoContenidoMensaje messageType, Conversacion session)
    {

        var title = "Bienvenido";
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

        string message = "¿En qué puedo ayudarte hoy?";

        var rows = new List<(string id, string title, string? description)>
        {
            ("menu_hacer_pedido", "🛒 Hacer pedido", "Realiza un nuevo pedido"),
            ("menu_estado_pedido", "📦 Estado de pedido", "Consulta tus pedidos recientes"),
            ("menu_solicitar_factura", "🧾 Solicitar factura", "Factura tu compra"),
            ("menu_informacion", "ℹ️ Información", "Horarios y ubicación")
        };

        await _whatsAppService.SendInteractiveListAsync(phoneNumber, greeting, "Ver Menú", rows, title, message);
        
        session.CambiarEstado(ConversationState.MENU);
        
        // Note: Caller is expected to save changes to session
    }
}
