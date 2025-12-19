using BotCarniceria.Infrastructure.Metrics;
using FluentAssertions;

namespace BotCarniceria.Infrastructure.Tests.Metrics;

public class ResilienceMetricsCollectorTests
{
    private readonly ResilienceMetricsCollector _sut;

    public ResilienceMetricsCollectorTests()
    {
        _sut = new ResilienceMetricsCollector();
    }

    [Fact]
    public void RecordSuccess_ShouldUpdateMetrics()
    {
        // Arrange
        var operation = "TestOperation";
        var latency = TimeSpan.FromMilliseconds(100);

        // Act
        _sut.RecordSuccess(operation, latency);

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.TotalRequests.Should().Be(1);
        metrics.SuccessfulRequests.Should().Be(1);
        metrics.FailedRequests.Should().Be(0);
        metrics.SuccessRate.Should().Be(100);
        metrics.AverageLatencyMs.Should().Be(100);
    }

    [Fact]
    public void RecordFailure_ShouldUpdateMetrics()
    {
        // Arrange
        var operation = "TestOperation";
        var latency = TimeSpan.FromMilliseconds(100);
        var error = "TestError";

        // Act
        _sut.RecordFailure(operation, latency, error);

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.TotalRequests.Should().Be(1);
        metrics.SuccessfulRequests.Should().Be(0);
        metrics.FailedRequests.Should().Be(1);
        metrics.ErrorRate.Should().Be(100);
        metrics.TopErrors.Should().ContainKey(error);
        metrics.TopErrors[error].Should().Be(1);
    }

    [Fact]
    public void RecordTimeout_ShouldIncrementTimeoutCount()
    {
        // Arrange
        var operation = "TestOperation";

        // Act
        _sut.RecordTimeout(operation);

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.TimeoutCount.Should().Be(1);
    }

    [Fact]
    public void RecordRetry_ShouldIncrementRetryCount()
    {
        // Arrange
        var operation = "TestOperation";

        // Act
        _sut.RecordRetry(operation, 1);

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.RetryCount.Should().Be(1);
    }

    [Fact]
    public void CircuitBreakerEvents_ShouldUpdateResilienceMetrics()
    {
        // Arrange
        var service = "TestService";

        // Act
        _sut.RecordCircuitBreakerOpened(service);
        _sut.RecordCircuitBreakerHalfOpened(service);
        _sut.RecordCircuitBreakerClosed(service);

        // Assert
        var metrics = _sut.GetResilienceMetrics();
        metrics.CircuitBreakerOpenCount.Should().Be(1);
        metrics.CircuitBreakerHalfOpenCount.Should().Be(1);
        metrics.CircuitBreakerCloseCount.Should().Be(1);
    }

    [Fact]
    public void GetMetrics_ShouldCalculateLatencyPercentiles()
    {
        // Arrange
        var operation = "TestOperation";
        // Add 100 requests with latencies 1ms to 100ms
        for (int i = 1; i <= 100; i++)
        {
            _sut.RecordSuccess(operation, TimeSpan.FromMilliseconds(i));
        }

        // Act
        var metrics = _sut.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(100);
        metrics.MinLatencyMs.Should().Be(1);
        metrics.MaxLatencyMs.Should().Be(100);
        metrics.MedianLatencyMs.Should().Be(50); // Percentile 50 of 1..100 is 50
        metrics.P95LatencyMs.Should().Be(95);
        metrics.P99LatencyMs.Should().Be(99);
        metrics.AverageLatencyMs.Should().Be(50.5);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _sut.RecordSuccess("Op", TimeSpan.FromSeconds(1));
        _sut.RecordFailure("Op", TimeSpan.FromSeconds(1), "Err");
        _sut.RecordTimeout("Op");
        
        // Act
        _sut.Reset();

        // Assert
        var metrics = _sut.GetMetrics();
        metrics.TotalRequests.Should().Be(0);
        metrics.RecentRequests.Should().Be(0);
        metrics.TimeoutCount.Should().Be(0);
    }
}
