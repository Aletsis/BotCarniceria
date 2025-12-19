namespace BotCarniceria.Core.Application.Interfaces.BackgroundJobs;

/// <summary>
/// Servicio para encolar trabajos en segundo plano
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Encola un trabajo para ejecución inmediata
    /// </summary>
    Task<string> EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// Programa un trabajo para ejecución futura
    /// </summary>
    Task<string> ScheduleAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// Programa un trabajo recurrente (cron)
    /// </summary>
    Task AddRecurringJobAsync<TJob>(string jobId, TJob job, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : class, IJob;

    /// <summary>
    /// Elimina un trabajo programado
    /// </summary>
    Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default);
}
