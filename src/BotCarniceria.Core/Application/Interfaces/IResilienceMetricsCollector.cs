namespace BotCarniceria.Core.Application.Interfaces;

/// <summary>
/// Interfaz para colectar métricas específicas de patrones de resiliencia
/// (Circuit Breaker, Retry, Timeout, etc.)
/// Pertenece a la capa de Application (Clean Architecture)
/// </summary>
public interface IResilienceMetricsCollector : IMetricsCollector
{
    /// <summary>
    /// Registra que un Circuit Breaker se abrió
    /// </summary>
    void RecordCircuitBreakerOpened(string serviceName);

    /// <summary>
    /// Registra que un Circuit Breaker pasó a Half-Open
    /// </summary>
    void RecordCircuitBreakerHalfOpened(string serviceName);

    /// <summary>
    /// Registra que un Circuit Breaker se cerró
    /// </summary>
    void RecordCircuitBreakerClosed(string serviceName);

    /// <summary>
    /// Obtiene métricas específicas de resiliencia
    /// </summary>
    ResilienceMetrics GetResilienceMetrics();
}

/// <summary>
/// DTO que representa métricas de patrones de resiliencia
/// Extiende OperationMetrics con información específica de Circuit Breaker
/// </summary>
public class ResilienceMetrics : OperationMetrics
{
    // Eventos del Circuit Breaker
    public long CircuitBreakerOpenCount { get; set; }
    public long CircuitBreakerHalfOpenCount { get; set; }
    public long CircuitBreakerCloseCount { get; set; }
    
    // Estado actual
    public string CurrentState { get; set; } = "Unknown";
    
    // Configuración
    public ResilienceConfiguration? Configuration { get; set; }
}

/// <summary>
/// Value Object que representa la configuración de resiliencia
/// </summary>
public class ResilienceConfiguration
{
    public double FailureRateThreshold { get; init; }
    public int DurationOfBreakInSeconds { get; init; }
    public int MaxRetries { get; init; }
    public int TimeoutInSeconds { get; init; }
    public int SamplingDurationInSeconds { get; init; }
    public int MinimumThroughput { get; init; }
}
