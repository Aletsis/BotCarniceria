using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Infrastructure.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Resilience;

public class WhatsAppServiceWithCircuitBreakerTests
{
    private readonly Mock<IWhatsAppService> _mockInnerService;
    private readonly Mock<ILogger<WhatsAppServiceWithCircuitBreaker>> _mockLogger;
    private readonly WhatsAppCircuitBreakerOptions _options;
    private readonly IResilienceMetricsCollector _metricsCollector;
    private readonly WhatsAppServiceWithCircuitBreaker _sut;

    public WhatsAppServiceWithCircuitBreakerTests()
    {
        _mockInnerService = new Mock<IWhatsAppService>();
        _mockLogger = new Mock<ILogger<WhatsAppServiceWithCircuitBreaker>>();
        _metricsCollector = new Infrastructure.Metrics.ResilienceMetricsCollector();
        
        // Configuración para pruebas: umbral bajo para que sea más fácil disparar el circuit breaker
        _options = new WhatsAppCircuitBreakerOptions
        {
            FailureThreshold = 3,
            DurationOfBreakInSeconds = 1,
            MaxRetries = 2,
            TimeoutInSeconds = 5,
            SamplingDurationInSeconds = 10,
            MinimumThroughput = 5,
            FailureRateThreshold = 50.0
        };

        var optionsWrapper = Options.Create(_options);
        _sut = new WhatsAppServiceWithCircuitBreaker(
            _mockInnerService.Object, 
            _mockLogger.Object, 
            optionsWrapper,
            _metricsCollector);
    }

    [Fact]
    public async Task SendTextMessageAsync_WhenInnerServiceSucceeds_ShouldReturnTrue()
    {
        // Arrange
        _mockInnerService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendTextMessageAsync("1234567890", "Test message");

        // Assert
        result.Should().BeTrue();
        _mockInnerService.Verify(x => x.SendTextMessageAsync("1234567890", "Test message"), Times.Once);
    }

    [Fact]
    public async Task SendTextMessageAsync_WhenInnerServiceFails_ShouldReturnFalse()
    {
        // Arrange
        _mockInnerService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _sut.SendTextMessageAsync("1234567890", "Test message");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task SendTextMessageAsync_WhenInnerServiceThrowsException_ShouldRetry()
    {
        // Arrange
        var callCount = 0;
        _mockInnerService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 2)
                    throw new HttpRequestException("API Error");
                return true;
            });

        // Act
        var result = await _sut.SendTextMessageAsync("1234567890", "Test message");

        // Assert
        result.Should().BeTrue();
        callCount.Should().Be(3); // 1 intento inicial + 2 reintentos
    }

    [Fact]
    public async Task SendInteractiveButtonsAsync_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var buttons = new List<(string id, string title)>
        {
            ("btn1", "Button 1"),
            ("btn2", "Button 2")
        };

        _mockInnerService
            .Setup(x => x.SendInteractiveButtonsAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<(string id, string title)>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendInteractiveButtonsAsync("1234567890", "Body text", buttons);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SendInteractiveListAsync_WhenSuccessful_ShouldReturnTrue()
    {
        // Arrange
        var rows = new List<(string id, string title, string? description)>
        {
            ("row1", "Row 1", "Description 1"),
            ("row2", "Row 2", "Description 2")
        };

        _mockInnerService
            .Setup(x => x.SendInteractiveListAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<List<(string id, string title, string? description)>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.SendInteractiveListAsync(
            "1234567890", 
            "Body text", 
            "Button text", 
            rows);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task MarkMessageAsReadAsync_WhenInnerServiceFails_ShouldNotThrowAndReturnFalse()
    {
        // Arrange
        _mockInnerService
            .Setup(x => x.MarkMessageAsReadAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act
        var result = await _sut.MarkMessageAsReadAsync("msg123");

        // Assert
        result.Should().BeFalse();
        // No debería lanzar excepción
    }

    [Fact]
    public async Task GetMetrics_ShouldReturnComprehensiveMetrics()
    {
        // Arrange - Simular algunas llamadas exitosas
        _mockInnerService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        // Act - Hacer algunas llamadas
        await _sut.SendTextMessageAsync("1234567890", "Test 1");
        await _sut.SendTextMessageAsync("1234567890", "Test 2");
        
        var metrics = _sut.GetMetrics();

        // Assert
        metrics.Should().NotBeNull();
        metrics.TotalRequests.Should().Be(2);
        metrics.SuccessfulRequests.Should().Be(2);
        metrics.FailedRequests.Should().Be(0);
        metrics.SuccessRate.Should().Be(100);
        metrics.ErrorRate.Should().Be(0);
        
        // Verificar que las métricas de latencia están presentes
        metrics.AverageLatencyMs.Should().BeGreaterThanOrEqualTo(0);
        metrics.MedianLatencyMs.Should().BeGreaterThanOrEqualTo(0);
        metrics.P95LatencyMs.Should().BeGreaterThanOrEqualTo(0);
        metrics.P99LatencyMs.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task CircuitBreaker_AfterMultipleFailures_ShouldContinueWorking()
    {
        // Arrange
        _mockInnerService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act - Generar suficientes fallos
        for (int i = 0; i < 5; i++)
        {
            await _sut.SendTextMessageAsync("1234567890", $"Message {i}");
        }

        // Assert - El servicio debe seguir respondiendo (aunque sea con false)
        var result = await _sut.SendTextMessageAsync("1234567890", "Final message");
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CircuitBreaker_WithExceptions_ShouldHandleGracefully()
    {
        // Arrange - Forzar excepciones
        _mockInnerService
            .Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new HttpRequestException("API Down"));

        // Act - Generar fallos
        for (int i = 0; i < 10; i++)
        {
            var result = await _sut.SendTextMessageAsync("1234567890", $"Message {i}");
            result.Should().BeFalse(); // Debe retornar false en lugar de lanzar excepción
        }

        // Assert - El servicio debe seguir manejando las llamadas sin lanzar excepciones
        var finalResult = await _sut.SendTextMessageAsync("1234567890", "Final message");
        finalResult.Should().BeFalse();
    }
}
