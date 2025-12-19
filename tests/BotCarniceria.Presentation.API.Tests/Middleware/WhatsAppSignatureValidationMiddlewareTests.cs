using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Presentation.API.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using FluentAssertions;

namespace BotCarniceria.Presentation.API.Tests.Middleware;

public class WhatsAppSignatureValidationMiddlewareTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<WhatsAppSignatureValidationMiddleware>> _mockLogger;

    public WhatsAppSignatureValidationMiddlewareTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<WhatsAppSignatureValidationMiddleware>>();
    }

    [Fact]
    public async Task InvokeAsync_NonWebhookPath_ShouldSkipValidation()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/other";
        context.Request.Method = "POST";
        bool nextCalled = false;
        
        var middleware = new WhatsAppSignatureValidationMiddleware(
            next: (innerHttpContext) => { nextCalled = true; return Task.CompletedTask; },
            logger: _mockLogger.Object
        );

        // Act
        await middleware.InvokeAsync(context, _mockUnitOfWork.Object);

        // Assert
        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_ValidSignature_ShouldCallNext()
    {
        // Arrange
        var bodyContent = "{\"test\":\"data\"}";
        var secret = "secret_key";
        var signature = GenerateSignature(bodyContent, secret);
        
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/webhook";
        context.Request.Method = "POST";
        context.Request.Headers["X-Hub-Signature-256"] = signature;
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
        context.Request.Body = stream;

        _mockUnitOfWork.Setup(u => u.Settings.GetValorAsync("WhatsApp_AppSecret"))
            .ReturnsAsync(secret);

        bool nextCalled = false;
        var middleware = new WhatsAppSignatureValidationMiddleware(
            next: (innerHttpContext) => { nextCalled = true; return Task.CompletedTask; },
            logger: _mockLogger.Object
        );

        // Act
        await middleware.InvokeAsync(context, _mockUnitOfWork.Object);

        // Assert
        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(401);
        
        // Verify body position reset
        context.Request.Body.Position.Should().Be(0);
    }

    [Fact]
    public async Task InvokeAsync_InvalidSignature_ShouldReturn401()
    {
        // Arrange
        var bodyContent = "{\"test\":\"data\"}";
        var secret = "secret_key";
        var invalidSignature = "sha256=invalid";
        
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/webhook";
        context.Request.Method = "POST";
        context.Request.Headers["X-Hub-Signature-256"] = invalidSignature;
        
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));
        context.Request.Body = stream;

        _mockUnitOfWork.Setup(u => u.Settings.GetValorAsync("WhatsApp_AppSecret"))
            .ReturnsAsync(secret);

        bool nextCalled = false;
        var middleware = new WhatsAppSignatureValidationMiddleware(
            next: (innerHttpContext) => { nextCalled = true; return Task.CompletedTask; },
            logger: _mockLogger.Object
        );

        // Act
        await middleware.InvokeAsync(context, _mockUnitOfWork.Object);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }
    
    [Fact]
    public async Task InvokeAsync_MissingSecret_ShouldReturn401()
    {
        // Arrange
        var bodyContent = "data";
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/webhook";
        context.Request.Method = "POST";
        context.Request.Headers["X-Hub-Signature-256"] = "sha256=something";
        context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(bodyContent));

        _mockUnitOfWork.Setup(u => u.Settings.GetValorAsync("WhatsApp_AppSecret"))
            .ReturnsAsync((string?)null); // No secret config

        bool nextCalled = false;
        var middleware = new WhatsAppSignatureValidationMiddleware(
            next: (c) => { nextCalled = true; return Task.CompletedTask; },
            logger: _mockLogger.Object
        );

        // Act
        await middleware.InvokeAsync(context, _mockUnitOfWork.Object);

        // Assert
        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(401);
    }

    private string GenerateSignature(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return "sha256=" + BitConverter.ToString(hash).Replace("-", "").ToUpper();
    }
}
