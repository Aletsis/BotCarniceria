namespace BotCarniceria.Core.Application.Interfaces.BackgroundJobs;

/// <summary>
/// Interfaz base para todos los trabajos en segundo plano
/// </summary>
public interface IJob
{
    /// <summary>
    /// Identificador único del trabajo
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Número máximo de reintentos
    /// </summary>
    int MaxRetries { get; }

    /// <summary>
    /// Prioridad del trabajo (0 = más alta)
    /// </summary>
    int Priority { get; }
}
