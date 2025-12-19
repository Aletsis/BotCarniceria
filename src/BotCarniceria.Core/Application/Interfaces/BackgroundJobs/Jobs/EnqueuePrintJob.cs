namespace BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;

/// <summary>
/// Trabajo para imprimir tickets en segundo plano
/// </summary>
public record EnqueuePrintJob : IJob
{
    public string JobId { get; init; } = Guid.NewGuid().ToString();
    public int MaxRetries { get; init; } = 3;
    public int Priority { get; init; } = 2;

    public required long PedidoId { get; init; }
    public required string PrinterName { get; init; }
    public bool PrintDuplicate { get; init; } = false;
    public int RetryCount { get; init; } = 0;
}
