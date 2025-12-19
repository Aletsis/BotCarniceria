using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Presentation.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives; // For StringValues
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Controllers;

public class WebhookControllerTests
{
    private readonly Mock<IIncomingMessageHandler> _mockHandler;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<WebhookController>> _mockLogger;
    private readonly WebhookController _controller;

    public WebhookControllerTests()
    {
        _mockHandler = new Mock<IIncomingMessageHandler>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<WebhookController>>();

        _controller = new WebhookController(_mockHandler.Object, _mockUnitOfWork.Object, _mockLogger.Object);
        
        // Setup default HttpContext
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public async Task VerifyToken_ValidToken_ShouldReturnOkWithChallenge()
    {
        // Arrange
        var token = "secret_token";
        var challenge = "123456";

        _mockUnitOfWork.Setup(u => u.Settings.GetValorAsync("WhatsApp_VerifyToken"))
            .ReturnsAsync(token);

        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "hub.mode", "subscribe" },
            { "hub.verify_token", token },
            { "hub.challenge", challenge }
        });
        
        _controller.Request.Query = query;

        // Act
        var result = await _controller.VerifyToken();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().Be(challenge);
    }

    [Fact]
    public async Task VerifyToken_InvalidToken_ShouldReturnForbid()
    {
        // Arrange
        var correctToken = "secret_token";
        _mockUnitOfWork.Setup(u => u.Settings.GetValorAsync("WhatsApp_VerifyToken"))
            .ReturnsAsync(correctToken);

        var query = new QueryCollection(new Dictionary<string, StringValues>
        {
            { "hub.mode", "subscribe" },
            { "hub.verify_token", "wrong_token" },
            { "hub.challenge", "123" }
        });

        _controller.Request.Query = query;

        // Act
        var result = await _controller.VerifyToken();

        // Assert
        result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task ReceiveMessage_ValidPayload_ShouldCallHandlerAndReturnOk()
    {
        // Arrange
        var message = new WhatsAppMessage { Id = "msg1" };
        var payload = new WebhookPayload
        {
            Entry = new List<WhatsAppEntry>
            {
                new WhatsAppEntry
                {
                    Changes = new List<WhatsAppChange>
                    {
                        new WhatsAppChange
                        {
                            Value = new WhatsAppValue
                            {
                                Messages = new List<WhatsAppMessage> { message }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = await _controller.ReceiveMessage(payload);

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockHandler.Verify(h => h.HandleAsync(message), Times.Once);
    }

    [Fact]
    public async Task ReceiveMessage_EmptyPayload_ShouldReturnOk_ButNoHandlerCall()
    {
        // Arrange
        // Usually if payload is null or entry empty, we return BadRequest according to logic in file.
        // File says: if (payload?.Entry == null) return BadRequest();
        
        var payload = new WebhookPayload(); // Entry is null

        // Act
        var result = await _controller.ReceiveMessage(payload);

        // Assert
        result.Should().BeOfType<BadRequestResult>();
        _mockHandler.Verify(h => h.HandleAsync(It.IsAny<WhatsAppMessage>()), Times.Never);
    }
}
