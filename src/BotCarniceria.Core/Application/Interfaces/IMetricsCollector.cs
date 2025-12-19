namespace BotCarniceria.Core.Application.Interfaces;

/// <summary>
/// Interfaz para colectar métricas de operaciones
/// Pertenece a la capa de Application (Clean Architecture)
/// </summary>
public interface IMetricsCollector
{
    /// <summary>
    /// Registra una operación exitosa con su latencia
    /// </summary>
    void RecordSuccess(string operationName, TimeSpan latency);

    /// <summary>
    /// Registra una operación fallida con su latencia y tipo de error
    /// </summary>
    void RecordFailure(string operationName, TimeSpan latency, string errorType);

    /// <summary>
    /// Registra un evento de timeout
    /// </summary>
    void RecordTimeout(string operationName);

    /// <summary>
    /// Registra un evento de reintento
    /// </summary>
    void RecordRetry(string operationName, int attemptNumber);

    /// <summary>
    /// Obtiene las métricas actuales
    /// </summary>
    OperationMetrics GetMetrics();

    /// <summary>
    /// Reinicia todas las métricas
    /// </summary>
    void Reset();
}

/// <summary>
/// DTO que representa las métricas de operaciones
/// Pertenece a la capa de Application
/// </summary>
public class OperationMetrics
{
    // Métricas totales
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public double SuccessRate { get; set; }
    public double ErrorRate { get; set; }
    
    // Métricas de latencia (en milisegundos)
    public double AverageLatencyMs { get; set; }
    public double MedianLatencyMs { get; set; }
    public double P95LatencyMs { get; set; }
    public double P99LatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    
    // Eventos
    public long TimeoutCount { get; set; }
    public long RetryCount { get; set; }
    
    // Métricas de ventana de tiempo
    public int RecentWindowMinutes { get; set; }
    public int RecentRequests { get; set; }
    public double RecentSuccessRate { get; set; }
    public double RecentErrorRate { get; set; }
    
    // Top errores
    public Dictionary<string, int> TopErrors { get; set; } = new();
    
    // Timestamp
    public DateTime Timestamp { get; set; }
}
