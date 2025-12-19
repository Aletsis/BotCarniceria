using System.Collections.Concurrent;
using BotCarniceria.Core.Application.Interfaces;

namespace BotCarniceria.Infrastructure.Metrics;

/// <summary>
/// Implementación concreta del colector de métricas de resiliencia
/// Pertenece a la capa de Infrastructure (Clean Architecture)
/// Implementa las interfaces definidas en Application
/// </summary>
public class ResilienceMetricsCollector : IResilienceMetricsCollector
{
    private readonly ConcurrentQueue<RequestMetric> _recentRequests = new();
    private readonly ConcurrentDictionary<string, long> _eventCounters = new();
    private readonly object _lock = new();
    
    private long _totalRequests;
    private long _successfulRequests;
    private long _failedRequests;
    private long _circuitBreakerOpenCount;
    private long _circuitBreakerHalfOpenCount;
    private long _circuitBreakerCloseCount;
    private long _timeoutCount;
    private long _retryCount;
    
    private const int MaxRecentRequests = 1000;
    private const int MetricsWindowMinutes = 5;

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

    public void RecordTimeout(string operationName)
    {
        Interlocked.Increment(ref _timeoutCount);
        IncrementEventCounter($"Timeout_{operationName}");
    }

    public void RecordRetry(string operationName, int attemptNumber)
    {
        Interlocked.Increment(ref _retryCount);
        IncrementEventCounter($"Retry_{operationName}_Attempt{attemptNumber}");
    }

    public void RecordCircuitBreakerOpened(string serviceName)
    {
        Interlocked.Increment(ref _circuitBreakerOpenCount);
        IncrementEventCounter($"CircuitBreakerOpened_{serviceName}");
    }

    public void RecordCircuitBreakerHalfOpened(string serviceName)
    {
        Interlocked.Increment(ref _circuitBreakerHalfOpenCount);
        IncrementEventCounter($"CircuitBreakerHalfOpened_{serviceName}");
    }

    public void RecordCircuitBreakerClosed(string serviceName)
    {
        Interlocked.Increment(ref _circuitBreakerCloseCount);
        IncrementEventCounter($"CircuitBreakerClosed_{serviceName}");
    }

    public OperationMetrics GetMetrics()
    {
        var recentMetrics = GetRecentMetrics();
        var latencyStats = CalculateLatencyStatistics(recentMetrics);
        
        return new OperationMetrics
        {
            TotalRequests = _totalRequests,
            SuccessfulRequests = _successfulRequests,
            FailedRequests = _failedRequests,
            SuccessRate = _totalRequests > 0 
                ? (double)_successfulRequests / _totalRequests * 100 
                : 100,
            ErrorRate = _totalRequests > 0 
                ? (double)_failedRequests / _totalRequests * 100 
                : 0,
            
            AverageLatencyMs = latencyStats.Average,
            MedianLatencyMs = latencyStats.Median,
            P95LatencyMs = latencyStats.P95,
            P99LatencyMs = latencyStats.P99,
            MinLatencyMs = latencyStats.Min,
            MaxLatencyMs = latencyStats.Max,
            
            TimeoutCount = _timeoutCount,
            RetryCount = _retryCount,
            
            RecentWindowMinutes = MetricsWindowMinutes,
            RecentRequests = recentMetrics.Count,
            RecentSuccessRate = recentMetrics.Count > 0
                ? (double)recentMetrics.Count(m => m.Success) / recentMetrics.Count * 100
                : 100,
            RecentErrorRate = recentMetrics.Count > 0
                ? (double)recentMetrics.Count(m => !m.Success) / recentMetrics.Count * 100
                : 0,
            
            TopErrors = GetTopErrors(recentMetrics),
            Timestamp = DateTime.UtcNow
        };
    }

    public ResilienceMetrics GetResilienceMetrics()
    {
        var baseMetrics = GetMetrics();
        
        return new ResilienceMetrics
        {
            // Copiar propiedades base
            TotalRequests = baseMetrics.TotalRequests,
            SuccessfulRequests = baseMetrics.SuccessfulRequests,
            FailedRequests = baseMetrics.FailedRequests,
            SuccessRate = baseMetrics.SuccessRate,
            ErrorRate = baseMetrics.ErrorRate,
            
            AverageLatencyMs = baseMetrics.AverageLatencyMs,
            MedianLatencyMs = baseMetrics.MedianLatencyMs,
            P95LatencyMs = baseMetrics.P95LatencyMs,
            P99LatencyMs = baseMetrics.P99LatencyMs,
            MinLatencyMs = baseMetrics.MinLatencyMs,
            MaxLatencyMs = baseMetrics.MaxLatencyMs,
            
            TimeoutCount = baseMetrics.TimeoutCount,
            RetryCount = baseMetrics.RetryCount,
            
            RecentWindowMinutes = baseMetrics.RecentWindowMinutes,
            RecentRequests = baseMetrics.RecentRequests,
            RecentSuccessRate = baseMetrics.RecentSuccessRate,
            RecentErrorRate = baseMetrics.RecentErrorRate,
            
            TopErrors = baseMetrics.TopErrors,
            Timestamp = baseMetrics.Timestamp,
            
            // Propiedades específicas de resiliencia
            CircuitBreakerOpenCount = _circuitBreakerOpenCount,
            CircuitBreakerHalfOpenCount = _circuitBreakerHalfOpenCount,
            CircuitBreakerCloseCount = _circuitBreakerCloseCount,
            CurrentState = DetermineCurrentState()
        };
    }

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
            _circuitBreakerCloseCount = 0;
            _timeoutCount = 0;
            _retryCount = 0;
        }
    }

    // Métodos privados de implementación
    private void AddRequestMetric(RequestMetric metric)
    {
        _recentRequests.Enqueue(metric);
        
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

    private string DetermineCurrentState()
    {
        // Lógica simple para determinar el estado basado en eventos recientes
        // En una implementación real, esto podría ser más sofisticado
        if (_circuitBreakerOpenCount > _circuitBreakerCloseCount)
        {
            return "Open";
        }
        else if (_circuitBreakerHalfOpenCount > _circuitBreakerCloseCount)
        {
            return "HalfOpen";
        }
        else
        {
            return "Closed";
        }
    }
}

/// <summary>
/// Métrica individual de una solicitud (detalle de implementación)
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
/// Estadísticas de latencia (detalle de implementación)
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
