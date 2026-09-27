using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Presentation.API.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Controllers;

public class WhatsAppTestControllerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly WhatsAppTestController _controller;

    public WhatsAppTestControllerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _controller = new WhatsAppTestController(_mockWhatsAppService.Object);
    }

    [Theory]
    [InlineData(null, "Mensaje de prueba")]
    [InlineData("", "Mensaje de prueba")]
    [InlineData("5551234567", null)]
    [InlineData("5551234567", "")]
    [InlineData(null, null)]
    [InlineData("", "")]
    public async Task SendText_WhenPhoneOrMessageIsNullOrEmpty_ShouldReturnBadRequest(string? phone, string? message)
    {
        // Act
        var result = await _controller.SendText(phone!, message!);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(400);
        badRequestResult.Value.Should().Be("Phone number and message are required.");
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SendText_WhenServiceReturnsTrue_ShouldReturnOk()
    {
        // Arrange
        var phone = "5551234567";
        var message = "Hola desde prueba";
        _mockWhatsAppService.Setup(w => w.SendTextMessageAsync(phone, message))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.SendText(phone, message);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
        okResult.Value.Should().Be("Message sent successfully.");
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(phone, message), Times.Once);
    }

    [Fact]
    public async Task SendText_WhenServiceReturnsFalse_ShouldReturnStatusCode500()
    {
        // Arrange
        var phone = "5551234567";
        var message = "Hola desde prueba";
        _mockWhatsAppService.Setup(w => w.SendTextMessageAsync(phone, message))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.SendText(phone, message);

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(500);
        statusResult.Value.Should().Be("Failed to send message. Check logs for details.");
    }
}
