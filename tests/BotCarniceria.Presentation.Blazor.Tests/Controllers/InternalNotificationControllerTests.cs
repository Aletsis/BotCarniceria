using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Presentation.Blazor.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Controllers;

public class InternalNotificationControllerTests
{
    private readonly Mock<IRealTimeNotificationService> _mockNotificationService;
    private readonly InternalNotificationController _controller;

    public InternalNotificationControllerTests()
    {
        _mockNotificationService = new Mock<IRealTimeNotificationService>();
        _controller = new InternalNotificationController(_mockNotificationService.Object);
    }

    [Fact]
    public async Task NotifyNewMessage_ShouldCallNotificationServiceAndReturnOk()
    {
        // Arrange
        var request = new NewMessageNotificationRequest
        {
            PhoneNumber = "5551234567",
            Message = "Nuevo pedido registrado"
        };

        // Act
        var result = await _controller.NotifyNewMessage(request);

        // Assert
        _mockNotificationService.Verify(
            s => s.NotifyNewMessageAsync("5551234567", "Nuevo pedido registrado"),
            Times.Once);

        result.Should().BeOfType<OkResult>();
    }
}
