using BotCarniceria.Presentation.Blazor.Hubs;
using BotCarniceria.Presentation.Blazor.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Services;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<ChatHub>> _mockHubContext;
    private readonly Mock<IHubClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _mockHubContext = new Mock<IHubContext<ChatHub>>();
        _mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        _mockHubContext.Setup(x => x.Clients).Returns(_mockClients.Object);
        _mockClients.Setup(x => x.All).Returns(_mockClientProxy.Object);
        _mockClients.Setup(x => x.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(_mockClientProxy.Object);

        _service = new SignalRNotificationService(_mockHubContext.Object);
    }

    [Fact]
    public async Task NotifyNewMessageAsync_ShouldSendReceiveMessageToAll()
    {
        // Arrange
        var phone = "1234567890";
        var message = "Hello World";

        // Act
        await _service.NotifyNewMessageAsync(phone, message);

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args => args.Length == 3 && (string)args[0] == phone && (string)args[1] == message),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOrdersUpdatedAsync_ShouldSendActualizarPedidosToAll()
    {
        // Act
        await _service.NotifyOrdersUpdatedAsync();

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ActualizarPedidos",
                It.Is<object[]>(args => args.Length == 0),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
