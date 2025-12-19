using System.Net.Http;
using System.Text;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Infrastructure.Services.External.WhatsApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Infrastructure.Services.External.WhatsApp;

public class WhatsAppService : IWhatsAppService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WhatsAppService> _logger;
    private readonly IRealTimeNotificationService _notificationService;
    private const string WhatsAppApiUrl = "https://graph.facebook.com/v18.0";
    private const int MaxRetries = 3;

    public WhatsAppService(
        IHttpClientFactory httpClientFactory,
        IUnitOfWork unitOfWork,
        ILogger<WhatsAppService> logger,
        IRealTimeNotificationService notificationService)
    {
        _httpClientFactory = httpClientFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _notificationService = notificationService;
    }

    private async Task<(string? PhoneNumberId, string? AccessToken)> GetCredentialsAsync()
    {
        var phoneNumberId = await _unitOfWork.Settings.GetValorAsync("WhatsApp_PhoneNumberId");
        var accessToken = await _unitOfWork.Settings.GetValorAsync("WhatsApp_AccessToken");
        return (phoneNumberId, accessToken);
    }

    public async Task<bool> SendTextMessageAsync(string phoneNumber, string message)
    {
        try
        {
            var payload = new MessagePayload
            {
                To = phoneNumber,
                Type = "text",
                Text = new TextPayload
                {
                    Body = message
                }
            };

            return await SendMessageAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar mensaje de texto a {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendInteractiveButtonsAsync(
        string phoneNumber,
        string bodyText,
        List<(string id, string title)> buttons,
        string? headerText = null,
        string? footerText = null)
    {
        try
        {
            if (buttons.Count > 3)
            {
                _logger.LogWarning("WhatsApp solo permite hasta 3 botones. Se tomarán los primeros 3.");
                buttons = buttons.Take(3).ToList();
            }

            var payload = new MessagePayload
            {
                To = phoneNumber,
                Type = "interactive",
                Interactive = new InteractivePayload
                {
                    Type = "button",
                    Header = !string.IsNullOrEmpty(headerText) ? new InteractiveHeader { Text = headerText.Length > 60 ? headerText.Substring(0, 60) : headerText } : null,
                    Body = new InteractiveBody { Text = bodyText.Length > 1024 ? bodyText.Substring(0, 1024) : bodyText },
                    Footer = !string.IsNullOrEmpty(footerText) ? new InteractiveFooter { Text = footerText.Length > 60 ? footerText.Substring(0, 60) : footerText } : null,
                    Action = new InteractiveAction
                    {
                        Buttons = buttons.Select(b => new InteractiveButton
                        {
                            Reply = new InteractiveReply
                            {
                                Id = b.id,
                                Title = b.title.Length > 20 ? b.title.Substring(0, 20) : b.title
                            }
                        }).ToList()
                    }
                }
            };

            return await SendMessageAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar botones interactivos a {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> SendInteractiveListAsync(
        string phoneNumber,
        string bodyText,
        string buttonText,
        List<(string id, string title, string? description)> rows,
        string? headerText = null,
        string? footerText = null)
    {
        try
        {
            if (rows.Count > 10)
            {
                _logger.LogWarning("WhatsApp solo permite hasta 10 opciones en una lista. Se tomarán las primeras 10.");
                rows = rows.Take(10).ToList();
            }

            var payload = new MessagePayload
            {
                To = phoneNumber,
                Type = "interactive",
                Interactive = new InteractivePayload
                {
                    Type = "list",
                    Header = !string.IsNullOrEmpty(headerText) ? new InteractiveHeader { Text = headerText.Length > 60 ? headerText.Substring(0, 60) : headerText } : null,
                    Body = new InteractiveBody { Text = bodyText.Length > 1024 ? bodyText.Substring(0, 1024) : bodyText },
                    Footer = !string.IsNullOrEmpty(footerText) ? new InteractiveFooter { Text = footerText.Length > 60 ? footerText.Substring(0, 60) : footerText } : null,
                    Action = new InteractiveAction
                    {
                        Button = buttonText,
                        Sections = new List<InteractiveSection>
                        {
                            new InteractiveSection
                            {
                                Rows = rows.Select(r => new InteractiveRow
                                {
                                    Id = r.id,
                                    Title = r.title.Length > 24 ? r.title.Substring(0, 24) : r.title,
                                    Description = r.description
                                }).ToList()
                            }
                        }
                    }
                }
            };

            return await SendMessageAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar lista interactiva a {PhoneNumber}", phoneNumber);
            return false;
        }
    }

    public async Task<bool> MarkMessageAsReadAsync(string messageId)
    {
        try
        {
            var (phoneNumberId, accessToken) = await GetCredentialsAsync();

            if (string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Configuración de WhatsApp incompleta (DB)");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{WhatsAppApiUrl}/{phoneNumberId}/messages";

            var payload = new
            {
                messaging_product = "whatsapp",
                status = "read",
                message_id = messageId
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Mensaje {MessageId} marcado como leído exitosamente", messageId);
                return true;
            }
            else
            {
                await HandleHttpErrorForMarkAsReadAsync(response.StatusCode, responseContent, messageId, 0);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al marcar mensaje como leído: {MessageId}", messageId);
            return false;
        }
    }

    private async Task<bool> SendMessageAsync(MessagePayload payload, int retryCount = 0, long? existingMessageId = null)
    {
        long? messageId = existingMessageId;
        Mensaje? outgoingMsg = null;

        try
        {
            // 1. Persist initial "Pendiente" message if not already done
            if (messageId == null)
            {
                string msgContent = "";
                TipoContenidoMensaje tipo = TipoContenidoMensaje.Texto;

                if (payload.Type == "text") 
                {
                    msgContent = payload.Text?.Body ?? "";
                }
                else if (payload.Type == "interactive") 
                {
                    msgContent = payload.Interactive?.Body?.Text ?? "[Mensaje Interactivo]";
                    tipo = TipoContenidoMensaje.Interactivo;
                }

                outgoingMsg = Mensaje.CrearSaliente(payload.To, msgContent, tipo);
                
                // Save JSON payload as metadata for debugging/resending
                var initialJson = JsonConvert.SerializeObject(payload, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                outgoingMsg.SetMetadata(initialJson);

                await _unitOfWork.Messages.AddAsync(outgoingMsg);
                await _unitOfWork.SaveChangesAsync();
                messageId = outgoingMsg.MensajeID;
            }

            var (phoneNumberId, accessToken) = await GetCredentialsAsync();

            if (string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Configuración de WhatsApp incompleta (DB)");
                if (messageId.HasValue) await MarkMessageAsFailedAsync(messageId.Value, "Configuración incompleta");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{WhatsAppApiUrl}/{phoneNumberId}/messages";

            var json = JsonConvert.SerializeObject(payload, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            _logger.LogInformation("Enviando mensaje a WhatsApp: {Json}", json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Mensaje enviado exitosamente a {PhoneNumber}", payload.To);
                
                // UPDATE MESSAGE TO SENT
                if (messageId.HasValue)
                {
                    var msgToUpdate = await _unitOfWork.Messages.GetByIdAsync(messageId.Value);
                    if (msgToUpdate != null)
                    {
                        try 
                        {
                            dynamic? jsonResponse = JsonConvert.DeserializeObject(responseContent);
                            string? waId = jsonResponse?.messages?[0]?.id;
                            msgToUpdate.MarcarComoEnviado(waId);
                        }
                        catch (Exception ex) 
                        { 
                            _logger.LogWarning("No se pudo parsear el ID de mensaje de WhatsApp: {Error}", ex.Message);
                            msgToUpdate.MarcarComoEnviado();
                        }
                        await _unitOfWork.SaveChangesAsync();
                    }
                }

                // Notify Frontend
                string notificationContent = "";
                if (payload.Type == "text") notificationContent = payload.Text?.Body ?? "Texto";
                else if (payload.Type == "interactive") notificationContent = payload.Interactive?.Body?.Text ?? "Interactivo";
                
                try 
                {
                   await _notificationService.NotifyNewMessageAsync(payload.To, notificationContent);
                }
                catch(Exception ex) 
                {
                     _logger.LogError(ex, "Error notificando SignalR tras enviar mensaje");
                }

                return true;
            }
            else
            {
                var shouldRetry = await HandleHttpErrorAsync(response.StatusCode, responseContent, payload.To, retryCount);
                
                if (shouldRetry && retryCount < MaxRetries)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                    return await SendMessageAsync(payload, retryCount + 1, messageId); // Pass messageId!
                }

                // Final Failure
                if (messageId.HasValue)
                {
                    await MarkMessageAsFailedAsync(messageId.Value, $"Error API {response.StatusCode}: {responseContent}");
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al enviar mensaje a WhatsApp");

            if (retryCount < MaxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                return await SendMessageAsync(payload, retryCount + 1, messageId);
            }

            if (messageId.HasValue)
            {
                await MarkMessageAsFailedAsync(messageId.Value, $"Excepción: {ex.Message}");
            }

            return false;
        }
    }

    private async Task MarkMessageAsFailedAsync(long messageId, string error)
    {
        try
        {
            var msg = await _unitOfWork.Messages.GetByIdAsync(messageId);
            if (msg != null)
            {
                msg.MarcarComoFallido(error);
                await _unitOfWork.SaveChangesAsync();
            }
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error al marcar mensaje como fallido en DB");
        }
    }

    private async Task<bool> HandleHttpErrorAsync(System.Net.HttpStatusCode statusCode, string responseContent, string phoneNumber, int retryCount)
    {
        // Simple error handling for now - can be expanded as needed
        if ((int)statusCode >= 500 || statusCode == System.Net.HttpStatusCode.TooManyRequests)
        {
            // Server error or rate limit - worth retrying
             _logger.LogWarning("WhatsApp API Error {StatusCode}: {Response}", statusCode, responseContent);
             return true;
        }
        
        _logger.LogError("WhatsApp API Fatal Error {StatusCode}: {Response}", statusCode, responseContent);
        return false;
    }

    private async Task<bool> HandleHttpErrorForMarkAsReadAsync(System.Net.HttpStatusCode statusCode, string responseContent, string messageId, int retryCount)
    {
         // Overload for MarkAsRead
         return await HandleHttpErrorAsync(statusCode, responseContent, "MessageID:" + messageId, retryCount);
    }
    public async Task<string?> DownloadMediaAsync(string mediaId)
    {
        try
        {
            var (phoneNumberId, accessToken) = await GetCredentialsAsync();
            if (string.IsNullOrEmpty(accessToken)) return null;

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            // 1. Get URL
            var urlResponse = await client.GetAsync($"{WhatsAppApiUrl}/{mediaId}");
            if (!urlResponse.IsSuccessStatusCode) 
            {
                _logger.LogWarning("Failed to get media URL for {MediaId}: {StatusCode}", mediaId, urlResponse.StatusCode);
                return null;
            }

            var urlJson = await urlResponse.Content.ReadAsStringAsync();
            dynamic? responseObj = JsonConvert.DeserializeObject(urlJson);
            string? downloadUrl = responseObj?.url;
            string? mimeType = responseObj?.mime_type;
            
            if (string.IsNullOrEmpty(downloadUrl)) return null;

            // 2. Download
            var mediaBytes = await client.GetByteArrayAsync(downloadUrl);

            // 3. Save
            string extension = GetExtension(mimeType ?? "");
            string fileName = $"{mediaId}{extension}";
            
            // Logic to save to Blazor wwwroot if possible (Dev environment hack)
            string savePath = GetSavePath(fileName);
            
            await File.WriteAllBytesAsync(savePath, mediaBytes);
            
            return $"/media/{fileName}"; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading media {MediaId}", mediaId);
            return null;
        }
    }

    private string GetSavePath(string fileName) 
    {
         // Start from Current Directory
         var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
         
         // Check if "BotCarniceria.Presentation.Blazor" is a sibling (Typical in Dev)
         if (dir.Parent != null)
         {
             var blazorDir = Path.Combine(dir.Parent.FullName, "BotCarniceria.Presentation.Blazor");
             if (Directory.Exists(blazorDir))
             {
                 var target = Path.Combine(blazorDir, "wwwroot", "media");
                 if (!Directory.Exists(target)) Directory.CreateDirectory(target);
                 return Path.Combine(target, fileName);
             }
         }
         
         // Fallback to local wwwroot
         var localTarget = Path.Combine(dir.FullName, "wwwroot", "media");
         if (!Directory.Exists(localTarget)) Directory.CreateDirectory(localTarget);
         return Path.Combine(localTarget, fileName);
    }

    public async Task<bool> ResendMessageAsync(string phoneNumber, string jsonPayload)
    {
        try
        {
            var (phoneNumberId, accessToken) = await GetCredentialsAsync();
            if (string.IsNullOrEmpty(phoneNumberId) || string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Configuración de WhatsApp incompleta (DB)");
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            var url = $"{WhatsAppApiUrl}/{phoneNumberId}/messages";

            // Ensure the payload is targeted to the correct user if needed, 
            // but assuming jsonPayload comes from last message to SAME user, it's fine.
            // Converting to JObject to verify/update 'to' would be safer but strictly not required if logic is sound.
            
            _logger.LogInformation("Re-enviando mensaje a WhatsApp: {Json}", jsonPayload);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

            var response = await client.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Mensaje re-enviado exitosamente a {PhoneNumber}", phoneNumber);
                
                // SAVE RESENT MESSAGE TO DB
                MessagePayload? payload = null;
                try 
                {
                    payload = JsonConvert.DeserializeObject<MessagePayload>(jsonPayload);
                }
                catch
                {
                    // If deserialization fails, we proceed with generic content
                }

                string msgContent = "[Contenido re-enviado]";
                if (payload != null)
                {
                    if (payload.Type == "text") msgContent = payload.Text?.Body ?? "";
                    else if (payload.Type == "interactive") msgContent = payload.Interactive?.Body?.Text ?? "[Mensaje Interactivo]";
                }
                
                var outgoingMsg = Mensaje.CrearSaliente(phoneNumber, msgContent, TipoContenidoMensaje.Texto);
                outgoingMsg.SetMetadata(jsonPayload); 

                try 
                {
                    dynamic? jsonResponse = JsonConvert.DeserializeObject(responseContent);
                    string? waId = jsonResponse?.messages?[0]?.id;
                    outgoingMsg.MarcarComoEnviado(waId);
                }
                catch (Exception ex) 
                { 
                    _logger.LogWarning("No se pudo parsear el ID de mensaje de WhatsApp (Resend): {Error}", ex.Message);
                    outgoingMsg.MarcarComoEnviado();
                }

                await _unitOfWork.Messages.AddAsync(outgoingMsg);
                await _unitOfWork.SaveChangesAsync();

                // Notify Frontend
                try 
                {
                   await _notificationService.NotifyNewMessageAsync(phoneNumber, msgContent);
                }
                catch(Exception ex) 
                {
                     _logger.LogError(ex, "Error notificando SignalR tras re-enviar mensaje");
                }

                return true;
            }
            else
            {
                _logger.LogWarning("Error al re-enviar: {StatusCode} - {Content}", response.StatusCode, responseContent);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Excepción al re-enviar mensaje a WhatsApp");
            return false;
        }
    }

    private string GetExtension(string mimeType)
    {
        if (mimeType.Contains("jpeg") || mimeType.Contains("jpg")) return ".jpg";
        if (mimeType.Contains("png")) return ".png";
        if (mimeType.Contains("webp")) return ".webp";
        if (mimeType.Contains("ogg")) return ".ogg"; // WhatsApp voice notes are usually ogg
        if (mimeType.Contains("mp3") || mimeType.Contains("mpeg")) return ".mp3";
        if (mimeType.Contains("pdf")) return ".pdf";
        if (mimeType.Contains("mp4")) return ".mp4";
        return "";
    }

}
