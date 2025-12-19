using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;

namespace BotCarniceria.Infrastructure.BackgroundJobs;

/// <summary>
/// Interfaz para handlers de trabajos en segundo plano
/// </summary>
/// <typeparam name="TJob">Tipo de trabajo a ejecutar</typeparam>
public interface IJobHandler<TJob> where TJob : class, IJob
{
    /// <summary>
    /// Ejecuta el trabajo
    /// </summary>
    Task ExecuteAsync(TJob job, CancellationToken cancellationToken);
}
