namespace BotCarniceria.Infrastructure.BackgroundJobs.Configuration;

/// <summary>
/// Opciones de configuración para Hangfire
/// </summary>
public class HangfireOptions
{
    public const string SectionName = "Hangfire";

    /// <summary>
    /// Cadena de conexión para almacenar trabajos
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Prefijo para tablas de Hangfire
    /// </summary>
    public string SchemaName { get; set; } = "hangfire";

    /// <summary>
    /// Número de workers concurrentes
    /// </summary>
    public int WorkerCount { get; set; } = 5;

    /// <summary>
    /// Habilitar dashboard
    /// </summary>
    public bool EnableDashboard { get; set; } = true;

    /// <summary>
    /// Ruta del dashboard
    /// </summary>
    public string DashboardPath { get; set; } = "/hangfire";

    /// <summary>
    /// Configuración de reintentos
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>
/// Opciones de configuración para reintentos
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Número máximo de reintentos por defecto
    /// </summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>
    /// Delay inicial en segundos
    /// </summary>
    public int InitialDelaySeconds { get; set; } = 10;

    /// <summary>
    /// Multiplicador para backoff exponencial
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Delay máximo en segundos
    /// </summary>
    public int MaxDelaySeconds { get; set; } = 3600; // 1 hora
}
