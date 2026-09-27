using System.Net;
using BotCarniceria.Presentation.API.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Services;

public class HttpRealTimeNotificationServiceTests
{
    private readonly Mock<ILogger<HttpRealTimeNotificationService>> _mockLogger;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

    public HttpRealTimeNotificationServiceTests()
    {
        _mockLogger = new Mock<ILogger<HttpRealTimeNotificationService>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        var httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
    }

    [Fact]
    public async Task NotifyNewMessageAsync_WhenServerReturnsSuccess_ShouldPostJsonPayload()
    {
        // Arrange
        var inMemory = new Dictionary<string, string?>
        {
            { "BlazorAppUrl", "http://myblazorapp.com" }
        };
        var config = new ConfigurationBuilder().AddInMemoryCollection(inMemory).Build();

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = new HttpRealTimeNotificationService(_mockHttpClientFactory.Object, _mockLogger.Object, config);

        // Act
        await service.NotifyNewMessageAsync("5551234567", "Hola mundo");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("http://myblazorapp.com/api/internal/notifications/message");
        capturedRequest.Method.Should().Be(HttpMethod.Post);

        var content = await capturedRequest.Content!.ReadAsStringAsync();
        content.Should().Contain("5551234567");
        content.Should().Contain("Hola mundo");
    }

    [Fact]
    public async Task NotifyNewMessageAsync_WhenServerReturnsFailure_ShouldLogWarning()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        var service = new HttpRealTimeNotificationService(_mockHttpClientFactory.Object, _mockLogger.Object, config);

        // Act
        await service.NotifyNewMessageAsync("5551234567", "Mensaje");

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to notify Blazor app")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNewMessageAsync_WhenHttpCallThrows_ShouldCatchAndLogError()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var service = new HttpRealTimeNotificationService(_mockHttpClientFactory.Object, _mockLogger.Object, config);

        // Act
        var act = async () => await service.NotifyNewMessageAsync("5551234567", "Mensaje");

        // Assert (should not throw)
        await act.Should().NotThrowAsync();

        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error notifying Blazor app via HTTP")),
                It.IsAny<HttpRequestException>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNewMessageAsync_WhenConfigUrlMissing_ShouldUseDefaultUrl()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        HttpRequestMessage? capturedRequest = null;
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var service = new HttpRealTimeNotificationService(_mockHttpClientFactory.Object, _mockLogger.Object, config);

        // Act
        await service.NotifyNewMessageAsync("5551234567", "Test");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.RequestUri!.ToString().Should().Be("http://localhost:5014/api/internal/notifications/message");
    }

    [Fact]
    public async Task Stubs_ShouldCompleteSuccessfully()
    {
        // Arrange
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        var service = new HttpRealTimeNotificationService(_mockHttpClientFactory.Object, _mockLogger.Object, config);

        // Act & Assert
        await service.NotifyOrdersUpdatedAsync();
        await service.NotifySessionExpiredAsync("5551234567");
        await service.NotifyOrderPrintedAsync("100");
        await service.NotifyUserTypingAsync("5551234567", true);
        await service.NotifyOrderPickedUpAsync("100");
    }
}
