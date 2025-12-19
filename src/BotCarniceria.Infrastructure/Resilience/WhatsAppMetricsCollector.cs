using System.Collections.Concurrent;
using System.Diagnostics;

namespace BotCarniceria.Infrastructure.Resilience;

/// <summary>
/// Colector de métricas para el servicio de WhatsApp
/// Rastrea latencia, tasa de error, y eventos del Circuit Breaker
/// </summary>
public class WhatsAppMetricsCollector
{
    private readonly ConcurrentQueue<RequestMetric> _recentRequests = new();
    private readonly ConcurrentDictionary<string, long> _eventCounters = new();
    private readonly object _lock = new();
    
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _circuitBreakerOpenCount;
    private long _circuitBreakerHalfOpenCount;
    private long _timeoutCount;
    private long _retryCount;
    
    private const int MaxRecentRequests = 1000; // Mantener últimas 1000 requests
    private const int MetricsWindowMinutes = 5; // Ventana de 5 minutos para métricas en tiempo real

    /// <summary>
    /// Registra una solicitud exitosa con su latencia
    /// </summary>
    public void RecordSuccess(string operationName, TimeSpan latency)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _successfulRequests);
        
        AddRequestMetric(new RequestMetric
        {
            OperationName = operationName,
            Success = true,
            Latency = latency,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Registra una solicitud fallida con su latencia y tipo de error
    /// </summary>
    public void RecordFailure(string operationName, TimeSpan latency, string errorType)
    {
        Interlocked.Increment(ref _totalRequests);
        Interlocked.Increment(ref _failedRequests);
        
        AddRequestMetric(new RequestMetric
        {
            OperationName = operationName,
            Success = false,
            Latency = latency,
            ErrorType = errorType,
            Timestamp = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Registra que el Circuit Breaker se abrió
    /// </summary>
    public void RecordCircuitBreakerOpened()
    {
        Interlocked.Increment(ref _circuitBreakerOpenCount);
        IncrementEventCounter("CircuitBreakerOpened");
    }

    /// <summary>
    /// Registra que el Circuit Breaker pasó a Half-Open
    /// </summary>
    public void RecordCircuitBreakerHalfOpened()
    {
        Interlocked.Increment(ref _circuitBreakerHalfOpenCount);
        IncrementEventCounter("CircuitBreakerHalfOpened");
    }

    /// <summary>
    /// Registra que el Circuit Breaker se cerró
    /// </summary>
    public void RecordCircuitBreakerClosed()
    {
        IncrementEventCounter("CircuitBreakerClosed");
    }

    /// <summary>
    /// Registra un timeout
    /// </summary>
    public void RecordTimeout()
    {
        Interlocked.Increment(ref _timeoutCount);
        IncrementEventCounter("Timeout");
    }

    /// <summary>
    /// Registra un reintento
    /// </summary>
    public void RecordRetry()
    {
        Interlocked.Increment(ref _retryCount);
        IncrementEventCounter("Retry");
    }

    /// <summary>
    /// Obtiene las métricas actuales
    /// </summary>
    public WhatsAppMetrics GetMetrics()
    {
        var recentMetrics = GetRecentMetrics();
        var latencyStats = CalculateLatencyStatistics(recentMetrics);
        
        return new WhatsAppMetrics
        {
            // Métricas totales (desde inicio)
            TotalRequests = _totalRequests,
            SuccessfulRequests = _successfulRequests,
            FailedRequests = _failedRequests,
            SuccessRate = _totalRequests > 0 
                ? (double)_successfulRequests / _totalRequests * 100 
                : 100,
            ErrorRate = _totalRequests > 0 
                ? (double)_failedRequests / _totalRequests * 100 
                : 0,
            
            // Métricas de latencia
            AverageLatencyMs = latencyStats.Average,
            MedianLatencyMs = latencyStats.Median,
            P95LatencyMs = latencyStats.P95,
            P99LatencyMs = latencyStats.P99,
            MinLatencyMs = latencyStats.Min,
            MaxLatencyMs = latencyStats.Max,
            
            // Eventos del Circuit Breaker
            CircuitBreakerOpenCount = _circuitBreakerOpenCount,
            CircuitBreakerHalfOpenCount = _circuitBreakerHalfOpenCount,
            TimeoutCount = _timeoutCount,
            RetryCount = _retryCount,
            
            // Métricas de ventana de tiempo (últimos 5 minutos)
            RecentWindowMinutes = MetricsWindowMinutes,
            RecentRequests = recentMetrics.Count,
            RecentSuccessRate = recentMetrics.Count > 0
                ? (double)recentMetrics.Count(m => m.Success) / recentMetrics.Count * 100
                : 100,
            RecentErrorRate = recentMetrics.Count > 0
                ? (double)recentMetrics.Count(m => !m.Success) / recentMetrics.Count * 100
                : 0,
            
            // Top errores
            TopErrors = GetTopErrors(recentMetrics),
            
            // Timestamp
            Timestamp = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Reinicia todas las métricas
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _recentRequests.Clear();
            _eventCounters.Clear();
            
            _totalRequests = 0;
            _successfulRequests = 0;
            _failedRequests = 0;
            _circuitBreakerOpenCount = 0;
            _circuitBreakerHalfOpenCount = 0;
            _timeoutCount = 0;
            _retryCount = 0;
        }
    }

    private void AddRequestMetric(RequestMetric metric)
    {
        _recentRequests.Enqueue(metric);
        
        // Mantener solo las últimas N requests
        while (_recentRequests.Count > MaxRecentRequests)
        {
            _recentRequests.TryDequeue(out _);
        }
    }

    private List<RequestMetric> GetRecentMetrics()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-MetricsWindowMinutes);
        return _recentRequests
            .Where(m => m.Timestamp >= cutoff)
            .ToList();
    }

    private LatencyStatistics CalculateLatencyStatistics(List<RequestMetric> metrics)
    {
        if (metrics.Count == 0)
        {
            return new LatencyStatistics();
        }

        var latencies = metrics
            .Select(m => m.Latency.TotalMilliseconds)
            .OrderBy(l => l)
            .ToList();

        return new LatencyStatistics
        {
            Average = latencies.Average(),
            Median = GetPercentile(latencies, 50),
            P95 = GetPercentile(latencies, 95),
            P99 = GetPercentile(latencies, 99),
            Min = latencies.Min(),
            Max = latencies.Max()
        };
    }

    private double GetPercentile(List<double> sortedValues, int percentile)
    {
        if (sortedValues.Count == 0) return 0;
        
        int index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
        index = Math.Max(0, Math.Min(sortedValues.Count - 1, index));
        
        return sortedValues[index];
    }

    private Dictionary<string, int> GetTopErrors(List<RequestMetric> metrics)
    {
        return metrics
            .Where(m => !m.Success && !string.IsNullOrEmpty(m.ErrorType))
            .GroupBy(m => m.ErrorType!)
            .OrderByDescending(g => g.Count())
            .Take(5)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private void IncrementEventCounter(string eventName)
    {
        _eventCounters.AddOrUpdate(eventName, 1, (_, count) => count + 1);
    }
}

/// <summary>
/// Métrica individual de una solicitud
/// </summary>
internal class RequestMetric
{
    public string OperationName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public TimeSpan Latency { get; set; }
    public string? ErrorType { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Estadísticas de latencia
/// </summary>
internal class LatencyStatistics
{
    public double Average { get; set; }
    public double Median { get; set; }
    public double P95 { get; set; }
    public double P99 { get; set; }
    public double Min { get; set; }
    public double Max { get; set; }
}

/// <summary>
/// Métricas completas del servicio de WhatsApp
/// </summary>
public class WhatsAppMetrics
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
    
    // Eventos del Circuit Breaker
    public long CircuitBreakerOpenCount { get; set; }
    public long CircuitBreakerHalfOpenCount { get; set; }
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
