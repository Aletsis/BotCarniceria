namespace BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;

/// <summary>
/// Trabajo para enviar mensajes de WhatsApp en segundo plano
/// </summary>
public record EnqueueWhatsAppMessageJob : IJob
{
    public string JobId { get; init; } = Guid.NewGuid().ToString();
    public int MaxRetries { get; init; } = 5;
    public int Priority { get; init; } = 1;

    public required string PhoneNumber { get; init; }
    public required string Message { get; init; }
    public string? MessageType { get; init; } = "text";
    public Dictionary<string, object>? AdditionalData { get; init; }
}
