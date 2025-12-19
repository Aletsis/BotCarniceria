using System.Net;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Infrastructure.Services.External.WhatsApp;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Entities;
using Newtonsoft.Json;

namespace BotCarniceria.Infrastructure.Tests.Services.External.WhatsApp;

public class WhatsAppServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMessageRepository> _mockMessageRepository;
    private readonly Mock<ILogger<WhatsAppService>> _mockLogger;
    private readonly Mock<IRealTimeNotificationService> _mockNotificationService;
    private readonly WhatsAppService _service;

    public WhatsAppServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<WhatsAppService>>();
        _mockNotificationService = new Mock<IRealTimeNotificationService>();

        // Setup Message Repository Mock
        _mockMessageRepository = new Mock<IMessageRepository>();
        _mockUnitOfWork.Setup(x => x.Messages).Returns(_mockMessageRepository.Object);
        _mockMessageRepository.Setup(x => x.AddAsync(It.IsAny<Mensaje>())).ReturnsAsync((Mensaje m) => m);

        var client = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("https://graph.facebook.com/v18.0")
        };
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        _service = new WhatsAppService(
            _mockHttpClientFactory.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object,
            _mockNotificationService.Object
        );
    }

    private void SetupCredentials(string phoneNumberId = "123456", string accessToken = "token")
    {
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync("WhatsApp_PhoneNumberId")).ReturnsAsync(phoneNumberId);
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync("WhatsApp_AccessToken")).ReturnsAsync(accessToken);
    }

    [Fact]
    public async Task SendTextMessageAsync_WithValidConfigAndSuccessResponse_ShouldReturnTrue()
    {
        // Arrange
        SetupCredentials();
        var phoneNumber = "5551234567";
        var message = "Hello";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        // Act
        var result = await _service.SendTextMessageAsync(phoneNumber, message);

        // Assert
        result.Should().BeTrue();
        _mockNotificationService.Verify(x => x.NotifyNewMessageAsync(phoneNumber, message), Times.Once);
        _mockUnitOfWork.Verify(x => x.Messages.AddAsync(It.IsAny<Mensaje>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendTextMessageAsync_WithMissingConfig_ShouldReturnFalse()
    {
        // Arrange
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync("WhatsApp_PhoneNumberId")).ReturnsAsync((string?)null);

        // Act
        var result = await _service.SendTextMessageAsync("5551234567", "Hello");

        // Assert
        result.Should().BeFalse();
        _mockUnitOfWork.Verify(x => x.Messages.AddAsync(It.IsAny<Mensaje>()), Times.Never);
    }

    [Fact]
    public async Task SendTextMessageAsync_WithApiErrorAndRetries_ShouldRetryAndReturnTrueWhenEventuallySucceeds()
    {
        // Arrange
        SetupCredentials();
        var phoneNumber = "5551234567";
        
        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError, Content = new StringContent("Error") })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.InternalServerError, Content = new StringContent("Error") })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        // Act
        var result = await _service.SendTextMessageAsync(phoneNumber, "Hello");

        // Assert
        result.Should().BeTrue();
        
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendTextMessageAsync_WithFatalError_ShouldNotRetryAndReturnFalse()
    {
        // Arrange
        SetupCredentials();
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage 
            { 
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad Request") 
            });

        // Act
        var result = await _service.SendTextMessageAsync("5551234567", "Hello");

        // Assert
        result.Should().BeFalse();
        
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendInteractiveButtonsAsync_ShouldSendCorrectTypeAndReturnTrue()
    {
        // Arrange
        SetupCredentials();
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        var buttons = new List<(string, string)> { ("id1", "Btn1"), ("id2", "Btn2") };

        // Act
        var result = await _service.SendInteractiveButtonsAsync("5551234567", "Body", buttons);

        // Assert
        result.Should().BeTrue();
        
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Content != null && req.Content.ReadAsStringAsync().Result.Contains("\"type\":\"interactive\"")
                && req.Content.ReadAsStringAsync().Result.Contains("\"type\":\"button\"")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _mockUnitOfWork.Verify(x => x.Messages.AddAsync(It.IsAny<Mensaje>()), Times.Once);
    }

    [Fact]
    public async Task SendInteractiveButtonsAsync_WithMoreThan3Buttons_ShouldTruncateTo3()
    {
        // Arrange
        SetupCredentials();
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        var buttons = new List<(string, string)> 
        { 
            ("id1", "Btn1"), 
            ("id2", "Btn2"),
            ("id3", "Btn3"),
            ("id4", "Btn4") // Should be truncated
        };

        // Act
        var result = await _service.SendInteractiveButtonsAsync("5551234567", "Body", buttons);

        // Assert
        result.Should().BeTrue();

        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Content != null && !req.Content.ReadAsStringAsync().Result.Contains("Btn4")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendInteractiveListAsync_ShouldSendCorrectTypeAndReturnTrue()
    {
        // Arrange
        SetupCredentials();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        var rows = new List<(string, string, string?)> { ("id1", "Title1", "Desc1"), ("id2", "Title2", "Desc2") };

        // Act
        var result = await _service.SendInteractiveListAsync("5551234567", "Body", "Button", rows);

        // Assert
        result.Should().BeTrue();

        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Content != null && req.Content.ReadAsStringAsync().Result.Contains("\"type\":\"interactive\"")
                && req.Content.ReadAsStringAsync().Result.Contains("\"type\":\"list\"")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
        _mockUnitOfWork.Verify(x => x.Messages.AddAsync(It.IsAny<Mensaje>()), Times.Once);
    }

    [Fact]
    public async Task SendInteractiveListAsync_WithMoreThan10Rows_ShouldTruncateTo10()
    {
        // Arrange
        SetupCredentials();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        var rows = Enumerable.Range(1, 15).Select(i => ($"id{i}", $"Title{i}", (string?)null)).ToList();

        // Act
        var result = await _service.SendInteractiveListAsync("5551234567", "Body", "Button", rows);

        // Assert
        result.Should().BeTrue();

        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Content != null && !req.Content.ReadAsStringAsync().Result.Contains("Title11")
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendTextMessageAsync_ShouldParseWhatsAppIdAndSaveToDb()
    {
        // Arrange
        SetupCredentials();
        var phoneNumber = "5551234567";
        var expectedWaId = "wamid.HBgL12345";
        var responseContent = $"{{\"messaging_product\":\"whatsapp\",\"contacts\":[],\"messages\":[{{\"id\":\"{expectedWaId}\"}}]}}";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseContent)
            });

        // Act
        var result = await _service.SendTextMessageAsync(phoneNumber, "test");

        // Assert
        result.Should().BeTrue();
        _mockUnitOfWork.Verify(x => x.Messages.AddAsync(It.Is<Mensaje>(m => m.WhatsAppMessageId == expectedWaId)), Times.Once);
    }

    [Fact]
    public async Task SendTextMessageAsync_With429_ShouldRetry()
    {
        // Arrange
        SetupCredentials();
        var phoneNumber = "5551234567";

        _mockHttpMessageHandler
            .Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.TooManyRequests, Content = new StringContent("Rate Limit") })
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        // Act
        var result = await _service.SendTextMessageAsync(phoneNumber, "Hello");

        // Assert
        result.Should().BeTrue();

        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>()
        );
    }

    [Fact]
    public async Task SendTextMessageAsync_WhenNotificationFails_ShouldStillReturnTrue()
    {
        // Arrange
        SetupCredentials();
        var phoneNumber = "5551234567";

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}")
            });

        _mockNotificationService
            .Setup(x => x.NotifyNewMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("SignalR Error"));

        // Act
        var result = await _service.SendTextMessageAsync(phoneNumber, "test");

        // Assert
        result.Should().BeTrue();
        // Verify notification was attempted
        _mockNotificationService.Verify(x => x.NotifyNewMessageAsync(phoneNumber, "test"), Times.Once);
        // Verify message still saved
        _mockUnitOfWork.Verify(x => x.Messages.AddAsync(It.IsAny<Mensaje>()), Times.Once);
    }

    [Fact]
    public async Task SendInteractiveButtonsAsync_ShouldTruncateBodyAndHeaders()
    {
        // Arrange
        SetupCredentials();
        
        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        var longHeader = new string('H', 70); // Limit 60
        var longBody = new string('B', 1050); // Limit 1024
        var buttons = new List<(string, string)> { ("id1", "Btn1") };

        // Act
        var result = await _service.SendInteractiveButtonsAsync("5551234567", longBody, buttons, longHeader);

        // Assert
        result.Should().BeTrue();
        
        _mockHttpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => 
                req.Content != null 
                && req.Content.ReadAsStringAsync().Result.Contains(new string('H', 60)) // Should appear
                && !req.Content.ReadAsStringAsync().Result.Contains(new string('H', 61)) // Should Not
                && req.Content.ReadAsStringAsync().Result.Contains(new string('B', 1024)) // Should appear
                && !req.Content.ReadAsStringAsync().Result.Contains(new string('B', 1025)) // Should Not
            ),
            ItExpr.IsAny<CancellationToken>()
        );
    }
    
    [Fact]
    public async Task MarkMessageAsReadAsync_ShouldReturnTrueOnSuccess()
    {
        // Arrange
        SetupCredentials();

        _mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent("{}") });

        // Act
        var result = await _service.MarkMessageAsReadAsync("msg_id_123");

        // Assert
        result.Should().BeTrue();
    }
}

