using BotCarniceria.Infrastructure.Resilience;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Resilience;

public class WhatsAppMetricsCollectorTests
{
    private readonly WhatsAppMetricsCollector _collector;

    public WhatsAppMetricsCollectorTests()
    {
        _collector = new WhatsAppMetricsCollector();
    }

    [Fact]
    public void GetMetrics_InitialState_ShouldReturnDefaultMetrics()
    {
        // Act
        var metrics = _collector.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(0);
        metrics.SuccessfulRequests.Should().Be(0);
        metrics.FailedRequests.Should().Be(0);
        metrics.SuccessRate.Should().Be(100);
        metrics.ErrorRate.Should().Be(0);
        metrics.CircuitBreakerOpenCount.Should().Be(0);
        metrics.CircuitBreakerHalfOpenCount.Should().Be(0);
        metrics.TimeoutCount.Should().Be(0);
        metrics.RetryCount.Should().Be(0);
        metrics.RecentRequests.Should().Be(0);
        metrics.RecentSuccessRate.Should().Be(100);
        metrics.RecentErrorRate.Should().Be(0);
        metrics.TopErrors.Should().BeEmpty();
    }

    [Fact]
    public void RecordSuccess_ShouldUpdateSuccessMetricsAndLatencies()
    {
        // Act
        _collector.RecordSuccess("SendTextMessage", TimeSpan.FromMilliseconds(50));
        _collector.RecordSuccess("SendTextMessage", TimeSpan.FromMilliseconds(150));
        var metrics = _collector.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(2);
        metrics.SuccessfulRequests.Should().Be(2);
        metrics.FailedRequests.Should().Be(0);
        metrics.SuccessRate.Should().Be(100);
        metrics.AverageLatencyMs.Should().Be(100);
        metrics.MinLatencyMs.Should().Be(50);
        metrics.MaxLatencyMs.Should().Be(150);
    }

    [Fact]
    public void RecordFailure_ShouldUpdateFailureMetricsAndTopErrors()
    {
        // Act
        _collector.RecordSuccess("SendTextMessage", TimeSpan.FromMilliseconds(100));
        _collector.RecordFailure("SendTextMessage", TimeSpan.FromMilliseconds(200), "TimeoutException");
        _collector.RecordFailure("SendTextMessage", TimeSpan.FromMilliseconds(300), "TimeoutException");
        _collector.RecordFailure("SendTextMessage", TimeSpan.FromMilliseconds(250), "HttpRequestException");

        var metrics = _collector.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(4);
        metrics.SuccessfulRequests.Should().Be(1);
        metrics.FailedRequests.Should().Be(3);
        metrics.SuccessRate.Should().Be(25.0);
        metrics.ErrorRate.Should().Be(75.0);
        metrics.TopErrors.Should().ContainKey("TimeoutException").WhoseValue.Should().Be(2);
        metrics.TopErrors.Should().ContainKey("HttpRequestException").WhoseValue.Should().Be(1);
    }

    [Fact]
    public void CircuitBreakerEvents_ShouldIncrementCounters()
    {
        // Act
        _collector.RecordCircuitBreakerOpened();
        _collector.RecordCircuitBreakerHalfOpened();
        _collector.RecordCircuitBreakerClosed();
        _collector.RecordTimeout();
        _collector.RecordRetry();

        var metrics = _collector.GetMetrics();

        // Assert
        metrics.CircuitBreakerOpenCount.Should().Be(1);
        metrics.CircuitBreakerHalfOpenCount.Should().Be(1);
        metrics.TimeoutCount.Should().Be(1);
        metrics.RetryCount.Should().Be(1);
    }

    [Fact]
    public void Reset_ShouldClearAllMetrics()
    {
        // Arrange
        _collector.RecordSuccess("SendTextMessage", TimeSpan.FromMilliseconds(100));
        _collector.RecordFailure("SendTextMessage", TimeSpan.FromMilliseconds(200), "Error");
        _collector.RecordTimeout();
        _collector.RecordCircuitBreakerOpened();

        // Act
        _collector.Reset();
        var metrics = _collector.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(0);
        metrics.SuccessfulRequests.Should().Be(0);
        metrics.FailedRequests.Should().Be(0);
        metrics.TimeoutCount.Should().Be(0);
        metrics.CircuitBreakerOpenCount.Should().Be(0);
        metrics.TopErrors.Should().BeEmpty();
    }

    [Fact]
    public void MaxRecentRequests_Capping_ShouldNotExceed1000()
    {
        // Act - enqueue 1050 requests
        for (int i = 0; i < 1050; i++)
        {
            _collector.RecordSuccess("Op", TimeSpan.FromMilliseconds(10));
        }

        var metrics = _collector.GetMetrics();

        // Assert
        metrics.TotalRequests.Should().Be(1050);
        metrics.RecentRequests.Should().Be(1000);
    }
}
