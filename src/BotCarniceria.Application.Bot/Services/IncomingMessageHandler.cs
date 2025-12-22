using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Application.Bot.Services;

public class IncomingMessageHandler : IIncomingMessageHandler
{
    private readonly IStateHandlerFactory _stateHandlerFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IRealTimeNotificationService _notificationService;
    private readonly ILogger<IncomingMessageHandler> _logger;

    public IncomingMessageHandler(
        IStateHandlerFactory stateHandlerFactory,
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        IRealTimeNotificationService notificationService,
        ILogger<IncomingMessageHandler> logger)
    {
        _stateHandlerFactory = stateHandlerFactory;
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task HandleAsync(WhatsAppMessage message)
    {
        var phoneNumber = message.From;
        if (string.IsNullOrEmpty(phoneNumber)) return;

        // Mark as read
        if (!string.IsNullOrEmpty(message.Id))
        {
            try 
            {
                await _whatsAppService.MarkMessageAsReadAsync(message.Id);
            }
            catch(Exception ex) 
            {
               _logger.LogWarning("Could not mark message as read: {Message}", ex.Message);
            }
        }

        string content = "";
        TipoContenidoMensaje tipoContenido = TipoContenidoMensaje.Texto;
        string? metadata = null;
        string typeStr = message.Type ?? "text";

        if (typeStr == "text")
        {
             content = message.Text?.Body ?? "";
             tipoContenido = TipoContenidoMensaje.Texto;
        }
        else if (typeStr == "interactive")
        {
             content = ExtractInteractiveContent(message);
             tipoContenido = TipoContenidoMensaje.Interactivo;
        }
        else if (typeStr == "location")
        {
             content = $"{message.Location?.Latitude},{message.Location?.Longitude}";
             tipoContenido = TipoContenidoMensaje.Ubicacion;
             metadata = message.Location?.Address; // Store address
        }
        else if (typeStr == "contacts")
        {
             if (message.Contacts != null && message.Contacts.Any())
             {
                 var contact = message.Contacts.FirstOrDefault();
                 content = $"{contact?.Name?.Formatted_Name} ({contact?.Phones?.FirstOrDefault()?.Phone})";
             }
             else
             {
                 content = "[Contacto]";
             }
             tipoContenido = TipoContenidoMensaje.Texto; 
        }
        else
        {
            // Media Types
            string? mediaId = null;
            string? caption = null;
            string? filename = null;
            string? mimeAndType = null;

            if (typeStr == "image") 
            { 
                mediaId = message.Image?.Id; 
                caption = message.Image?.Caption; 
                tipoContenido = TipoContenidoMensaje.Imagen;
                mimeAndType = message.Image?.Mime_Type;
            }
            else if (typeStr == "audio") 
            { 
                mediaId = message.Audio?.Id; 
                tipoContenido = TipoContenidoMensaje.Audio;
                mimeAndType = message.Audio?.Mime_Type;
            } 
            else if (typeStr == "voice") 
            { 
                mediaId = message.Audio?.Id; 
                tipoContenido = TipoContenidoMensaje.Audio;
                mimeAndType = message.Audio?.Mime_Type;
            }
            else if (typeStr == "video") 
            { 
                mediaId = message.Video?.Id; 
                caption = message.Video?.Caption; 
                tipoContenido = TipoContenidoMensaje.Documento; // Fallback to Documento as Video enum missing
                mimeAndType = message.Video?.Mime_Type;
            }
            else if (typeStr == "document") 
            { 
                mediaId = message.Document?.Id; 
                caption = message.Document?.Caption; 
                filename = message.Document?.Filename; 
                tipoContenido = TipoContenidoMensaje.Documento;
                mimeAndType = message.Document?.Mime_Type;
            }
            else if (typeStr == "sticker") 
            { 
                mediaId = message.Sticker?.Id; 
                tipoContenido = TipoContenidoMensaje.Imagen; // Fallback to Imagen
                mimeAndType = message.Sticker?.Mime_Type;
            }
            
            if (!string.IsNullOrEmpty(mediaId))
            {
                 var path = await _whatsAppService.DownloadMediaAsync(mediaId);
                 if (path != null)
                 {
                     content = path; // The relative URL
                     // Store metadata
                     var metaObj = new Dictionary<string, string?>
                     {
                        { "Caption", caption },
                        { "Filename", filename },
                        { "OriginalId", mediaId },
                        { "MimeType", mimeAndType }
                     };
                     metadata = System.Text.Json.JsonSerializer.Serialize(metaObj);
                 }
                 else
                 {
                     content = $"[Error descargando {typeStr}]";
                 }
            }
            else
            {
                content = $"[Tipo no soportado: {typeStr}]";
            }
        }
        
        if (string.IsNullOrEmpty(content)) return;

        // Notify Real-time (Frontend)
        await _notificationService.NotifyNewMessageAsync(phoneNumber, content);

        // SAVE INCOMING MESSAGE
        var incomingMsg = Mensaje.CrearEntrante(phoneNumber, content, tipoContenido, message.Id, metadata);
        await _unitOfWork.Messages.AddAsync(incomingMsg);
        
        // Get Session or Create
        var session = await _unitOfWork.Sessions.GetByPhoneAsync(phoneNumber);
        if (session == null)
        {
            session = Conversacion.Create(phoneNumber);
            await _unitOfWork.Sessions.AddAsync(session);
        }

        // Global Commands
        if (content.Trim().ToLower() == "menu" || content.Trim().ToLower() == "cancelar" || content.Trim().ToLower() == "inicio")
        {
            session.CambiarEstado(ConversationState.MENU);
            session.LimpiarBuffer();
        }

        // Handle Unsupported Types
        if (IsUnsupportedType(typeStr))
        {
            await HandleUnsupportedMessageAsync(typeStr, phoneNumber);
            return;
        }

        // Processing
        try
        {
            // Update Activity
            session.ActualizarActividad();

            var handler = _stateHandlerFactory.GetHandler(session.Estado);
            await handler.HandleAsync(phoneNumber, content, tipoContenido, session);

            // Save changes from state machine processing
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from {PhoneNumber}", phoneNumber);
            // Optionally update message status to Error if we tracked it
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "❌ Ocurrió un error. Escribe 'menu' para reiniciar.");
        }
    }

    // Removed ExtractContent as it is now integrated into HandleAsync

    private string ExtractInteractiveContent(WhatsAppMessage message)
    {
        if (message.Interactive?.Button_Reply != null) return message.Interactive.Button_Reply.Id ?? "";
        if (message.Interactive?.List_Reply != null) return message.Interactive.List_Reply.Id ?? "";
        return "";
    }

    private bool IsUnsupportedType(string typeStr)
    {
        return typeStr switch
        {
            "image" or "document" or "location" or "contacts" or "sticker" or "audio" or "voice" => true,
            _ => false
        };
    }

    private async Task HandleUnsupportedMessageAsync(string typeStr, string phoneNumber)
    {
        string msg = typeStr switch
        {
            "image" => "Lo siento aun no tengo soporte para leer Imágenes",
            "document" => "Lo siento aun no tengo soporte para leer Documentos",
            "location" => "Lo siento aun no tengo soporte para leer Ubicaciones",
            "contacts" => "Lo siento aun no tengo soporte para leer Contactos",
            "sticker" => "Lo siento aun no tengo soporte para leer Stickers",
            "audio" or "voice" => "Lo siento aun no tengo soporte para escuchar Audios",
            _ => "Lo siento, formato no soportado."
        };

        // 1. Fetch Last Outgoing Message *BEFORE* sending the apology
        // We fetch last 20 messages to ensure we find the last outgoing one
        var messages = await _unitOfWork.Messages.GetByPhoneAsync(phoneNumber, count: 20);
        
        // Find last message where Origin is Saliente (Outgoing)
        // Since GetByPhoneAsync returns chronological list (Old -> New), we search from end.
        var lastOutgoing = messages.LastOrDefault(m => m.Origen == TipoMensajeOrigen.Saliente);

        // 2. Send Apology
        await _whatsAppService.SendTextMessageAsync(phoneNumber, msg);

        // 3. Resend Last Outgoing Message (if found)
        if (lastOutgoing != null)
        {
            if (!string.IsNullOrEmpty(lastOutgoing.MetadataWhatsApp) && lastOutgoing.MetadataWhatsApp.TrimStart().StartsWith("{"))
            {
                // Attempt to resend exact payload (includes buttons, lists, formatting)
                await _whatsAppService.ResendMessageAsync(phoneNumber, lastOutgoing.MetadataWhatsApp);
            }
            else if (!string.IsNullOrEmpty(lastOutgoing.Contenido))
            {
                // Fallback to text content
                await _whatsAppService.SendTextMessageAsync(phoneNumber, lastOutgoing.Contenido);
            }
        }
    }
}
