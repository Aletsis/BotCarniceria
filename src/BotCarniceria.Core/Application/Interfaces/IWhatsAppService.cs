using BotCarniceria.Core.Application.DTOs;

namespace BotCarniceria.Core.Application.Interfaces;

public interface IWhatsAppService
{
    Task<bool> SendTextMessageAsync(string phoneNumber, string message);
    Task<bool> SendInteractiveButtonsAsync(string phoneNumber, string bodyText, List<(string id, string title)> buttons, string? headerText = null, string? footerText = null);
    Task<bool> SendInteractiveListAsync(string phoneNumber, string bodyText, string buttonText, List<(string id, string title, string? description)> rows, string? headerText = null, string? footerText = null);
    Task<bool> MarkMessageAsReadAsync(string messageId);

    // DTO overloads (we might need to define these DTOs or just use the primitive parameters if DTOs are not in Core Impl)
    // For now, I'll stick to primitives to minimize dependencies, or move Request objects to DTOs.
    Task<string?> DownloadMediaAsync(string mediaId);
    Task<bool> ResendMessageAsync(string phoneNumber, string jsonPayload);
}
