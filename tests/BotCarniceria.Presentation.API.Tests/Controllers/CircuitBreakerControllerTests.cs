using System.Text.Json;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Infrastructure.Resilience;
using BotCarniceria.Presentation.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Controllers;

public class CircuitBreakerControllerTests
{
    private readonly Mock<ILogger<CircuitBreakerController>> _mockLogger;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly CircuitBreakerController _controller;

    public CircuitBreakerControllerTests()
    {
        _mockLogger = new Mock<ILogger<CircuitBreakerController>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _controller = new CircuitBreakerController(_mockLogger.Object, _mockServiceProvider.Object);
    }

    private WhatsAppServiceWithCircuitBreaker CreateConfiguredCircuitBreakerService(ResilienceMetrics metrics)
    {
        var mockInner = new Mock<IWhatsAppService>();
        var mockServiceLogger = new Mock<ILogger<WhatsAppServiceWithCircuitBreaker>>();
        var options = Options.Create(new WhatsAppCircuitBreakerOptions
        {
            FailureRateThreshold = 50,
            SamplingDurationInSeconds = 30,
            DurationOfBreakInSeconds = 30,
            MaxRetries = 3,
            TimeoutInSeconds = 10
        });
        var mockMetricsCollector = new Mock<IResilienceMetricsCollector>();
        mockMetricsCollector.Setup(m => m.GetResilienceMetrics()).Returns(metrics);

        return new WhatsAppServiceWithCircuitBreaker(
            mockInner.Object,
            mockServiceLogger.Object,
            options,
            mockMetricsCollector.Object);
    }

    #region GetWhatsAppCircuitBreakerStatus Tests

    [Fact]
    public void GetWhatsAppCircuitBreakerStatus_WhenConfigured_ShouldReturnOkWithMetrics()
    {
        // Arrange
        var metrics = new ResilienceMetrics
        {
            TotalRequests = 100,
            SuccessfulRequests = 90,
            FailedRequests = 10,
            SuccessRate = 90.0,
            ErrorRate = 10.0,
            AverageLatencyMs = 120.5,
            MedianLatencyMs = 110.0,
            P95LatencyMs = 250.0,
            P99LatencyMs = 300.0,
            MinLatencyMs = 40.0,
            MaxLatencyMs = 450.0,
            CircuitBreakerOpenCount = 1,
            CircuitBreakerHalfOpenCount = 1,
            TimeoutCount = 2,
            RetryCount = 3,
            RecentWindowMinutes = 5,
            RecentRequests = 20,
            RecentSuccessRate = 95.0,
            RecentErrorRate = 5.0,
            Timestamp = DateTime.UtcNow,
            TopErrors = new Dictionary<string, int> { { "TimeoutException", 2 } }
        };

        var service = CreateConfiguredCircuitBreakerService(metrics);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns(service);

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        doc.RootElement.GetProperty("CircuitBreakerActive").GetBoolean().Should().BeTrue();
        doc.RootElement.GetProperty("State").GetString().Should().Be("Closed");
        doc.RootElement.GetProperty("Message").GetString().Should().Contain("operando normalmente");

        var total = doc.RootElement.GetProperty("TotalMetrics");
        total.GetProperty("TotalRequests").GetInt64().Should().Be(100);
        total.GetProperty("SuccessfulRequests").GetInt64().Should().Be(90);
        total.GetProperty("FailedRequests").GetInt64().Should().Be(10);
    }

    [Fact]
    public void GetWhatsAppCircuitBreakerStatus_WhenNotConfigured_ShouldReturnOkWithInactiveStatus()
    {
        // Arrange
        var mockPlainService = new Mock<IWhatsAppService>();
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns(mockPlainService.Object);

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        doc.RootElement.GetProperty("CircuitBreakerActive").GetBoolean().Should().BeFalse();
        doc.RootElement.GetProperty("Message").GetString().Should().Contain("no está configurado");
    }

    [Fact]
    public void GetWhatsAppCircuitBreakerStatus_WhenNullService_ShouldReturnOkWithInactiveStatus()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns((object?)null);

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        doc.RootElement.GetProperty("CircuitBreakerActive").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public void GetWhatsAppCircuitBreakerStatus_WhenExceptionThrown_ShouldReturn500()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Throws(new InvalidOperationException("Dependency failure"));

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerStatus();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error al obtener estado")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetWhatsAppCircuitBreakerConfig Tests

    [Fact]
    public void GetWhatsAppCircuitBreakerConfig_WhenConfigured_ShouldReturnOkWithConfig()
    {
        // Arrange
        var metrics = new ResilienceMetrics { SuccessRate = 95.0 };
        var service = CreateConfiguredCircuitBreakerService(metrics);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns(service);

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerConfig();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        var config = doc.RootElement.GetProperty("Configuration");
        config.GetProperty("DurationOfBreak").GetString().Should().Be("30 segundos");
        config.GetProperty("MaxRetries").GetInt32().Should().Be(3);
    }

    [Fact]
    public void GetWhatsAppCircuitBreakerConfig_WhenNotConfigured_ShouldReturnNotFound()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns((object?)null);

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerConfig();

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void GetWhatsAppCircuitBreakerConfig_WhenExceptionThrown_ShouldReturn500()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Throws(new Exception("Config error"));

        // Act
        var result = _controller.GetWhatsAppCircuitBreakerConfig();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion

    #region GetWhatsAppMetrics Tests

    [Fact]
    public void GetWhatsAppMetrics_WhenConfigured_ShouldReturnOkWithPrometheusMetrics()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var metrics = new ResilienceMetrics
        {
            TotalRequests = 50,
            SuccessfulRequests = 48,
            FailedRequests = 2,
            CircuitBreakerOpenCount = 0,
            CircuitBreakerHalfOpenCount = 0,
            TimeoutCount = 1,
            RetryCount = 2,
            SuccessRate = 96.0,
            ErrorRate = 4.0,
            RecentSuccessRate = 96.0,
            RecentErrorRate = 4.0,
            AverageLatencyMs = 85.0,
            MedianLatencyMs = 80.0,
            P95LatencyMs = 150.0,
            P99LatencyMs = 180.0,
            MinLatencyMs = 30.0,
            MaxLatencyMs = 200.0,
            Timestamp = now,
            RecentWindowMinutes = 5,
            RecentRequests = 10
        };

        var service = CreateConfiguredCircuitBreakerService(metrics);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns(service);

        // Act
        var result = _controller.GetWhatsAppMetrics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(okResult.Value));
        doc.RootElement.GetProperty("whatsapp_requests_total").GetInt64().Should().Be(50);
        doc.RootElement.GetProperty("whatsapp_requests_successful").GetInt64().Should().Be(48);
        doc.RootElement.GetProperty("whatsapp_requests_failed").GetInt64().Should().Be(2);
        doc.RootElement.GetProperty("whatsapp_latency_average_ms").GetDouble().Should().Be(85.0);
    }

    [Fact]
    public void GetWhatsAppMetrics_WhenNotConfigured_ShouldReturnNotFound()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Returns((object?)null);

        // Act
        var result = _controller.GetWhatsAppMetrics();

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);
    }

    [Fact]
    public void GetWhatsAppMetrics_WhenExceptionThrown_ShouldReturn500()
    {
        // Arrange
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IWhatsAppService)))
            .Throws(new Exception("Metrics crash"));

        // Act
        var result = _controller.GetWhatsAppMetrics();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
    }

    #endregion
}
