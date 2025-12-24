using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;

namespace BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;

public class ProcessIncomingMessageJob : IJob
{
    public WhatsAppMessage Message { get; set; } = null!;

    public string JobId => Message?.Id ?? Guid.NewGuid().ToString();
    public int MaxRetries => 3;
    public int Priority => 0;
}
