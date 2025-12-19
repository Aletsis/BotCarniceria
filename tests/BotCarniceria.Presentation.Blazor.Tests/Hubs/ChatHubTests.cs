using BotCarniceria.Presentation.Blazor.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Hubs;

public class ChatHubTests
{
    private readonly Mock<IHubCallerClients> _mockClients;
    private readonly Mock<IClientProxy> _mockClientProxy;
    private readonly Mock<HubCallerContext> _mockContext;
    private readonly Mock<IGroupManager> _mockGroups;
    private readonly Mock<ILogger<ChatHub>> _mockLogger;
    private readonly ChatHub _hub;

    public ChatHubTests()
    {
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();
        _mockLogger = new Mock<ILogger<ChatHub>>();

        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(_mockClientProxy.Object);

        _hub = new ChatHub(_mockLogger.Object)
        {
            Clients = _mockClients.Object,
            Context = _mockContext.Object,
            Groups = _mockGroups.Object
        };
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToAll()
    {
        // Act
        await _hub.SendMessage("user", "msg");

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args => (string)args[0] == "user" && (string)args[1] == "msg"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinConversation_ShouldAddToGroup()
    {
        // Arrange
        _mockContext.Setup(c => c.ConnectionId).Returns("connId");

        // Act
        await _hub.JoinConversation("123");

        // Assert
        _mockGroups.Verify(x => x.AddToGroupAsync("connId", "123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task NotifyDashboardUpdate_ShouldBroadcast()
    {
        // Act
        await _hub.NotifyDashboardUpdate();

        // Assert
        _mockClientProxy.Verify(
             x => x.SendCoreAsync(
                 "DashboardUpdate",
                 It.IsAny<object[]>(),
                 It.IsAny<CancellationToken>()),
             Times.Once);
    }
}
