namespace BotCarniceria.Infrastructure.Resilience;

/// <summary>
/// Opciones de configuración para el Circuit Breaker de WhatsApp API
/// </summary>
public class WhatsAppCircuitBreakerOptions
{
    public const string SectionName = "WhatsAppCircuitBreaker";

    /// <summary>
    /// Número de fallos consecutivos antes de abrir el circuito
    /// </summary>
    public int FailureThreshold { get; set; } = 5;

    /// <summary>
    /// Duración en segundos que el circuito permanece abierto antes de pasar a half-open
    /// </summary>
    public int DurationOfBreakInSeconds { get; set; } = 30;

    /// <summary>
    /// Número máximo de reintentos para errores transitorios
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Tiempo de espera en segundos para cada llamada HTTP
    /// </summary>
    public int TimeoutInSeconds { get; set; } = 10;

    /// <summary>
    /// Duración de la ventana de muestreo en segundos para el circuit breaker avanzado
    /// </summary>
    public int SamplingDurationInSeconds { get; set; } = 60;

    /// <summary>
    /// Número mínimo de llamadas en la ventana de muestreo antes de evaluar el circuit breaker
    /// </summary>
    public int MinimumThroughput { get; set; } = 10;

    /// <summary>
    /// Porcentaje de fallos que dispara el circuit breaker (0-100)
    /// </summary>
    public double FailureRateThreshold { get; set; } = 50.0;
}
