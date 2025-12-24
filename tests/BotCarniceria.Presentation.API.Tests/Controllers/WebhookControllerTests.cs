using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Presentation.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Controllers;

public class WebhookControllerTests
{
    private readonly Mock<IBackgroundJobService> _mockJobService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<WebhookController>> _mockLogger;
    private readonly WebhookController _controller;

    public WebhookControllerTests()
    {
        _mockJobService = new Mock<IBackgroundJobService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<WebhookController>>();

        _controller = new WebhookController(
            _mockJobService.Object, 
            _mockCacheService.Object, 
            _mockUnitOfWork.Object, 
            _mockLogger.Object);
        
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
        // Content helper returns ContentResult
        var contentResult = result.Should().BeOfType<ContentResult>().Subject;
        contentResult.Content.Should().Be(challenge);
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
    public async Task ReceiveMessage_ValidPayload_NewMessage_ShouldEnqueueAndReturnOk()
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

        _mockCacheService.Setup(c => c.ExistsAsync($"webhook_msg_{message.Id}")).ReturnsAsync(false);

        // Act
        var result = await _controller.ReceiveMessage(payload);

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockJobService.Verify(j => j.EnqueueAsync(It.Is<ProcessIncomingMessageJob>(job => job.Message == message), default), Times.Once);
        _mockCacheService.Verify(c => c.SetAsync($"webhook_msg_{message.Id}", "processed", It.IsAny<TimeSpan?>()), Times.Once);
    }

    [Fact]
    public async Task ReceiveMessage_DuplicateMessage_ShouldSkipEnqueue()
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

        _mockCacheService.Setup(c => c.ExistsAsync($"webhook_msg_{message.Id}")).ReturnsAsync(true);

        // Act
        var result = await _controller.ReceiveMessage(payload);

        // Assert
        result.Should().BeOfType<OkResult>();
        _mockJobService.Verify(j => j.EnqueueAsync(It.IsAny<ProcessIncomingMessageJob>(), default), Times.Never);
    }
}
