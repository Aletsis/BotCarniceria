using System.Security.Claims;
using BotCarniceria.Core.Domain.Services;
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
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly ChatHub _hub;
    private readonly DateTime _fixedNow = new(2026, 9, 27, 12, 0, 0);

    public ChatHubTests()
    {
        _mockClients = new Mock<IHubCallerClients>();
        _mockClientProxy = new Mock<IClientProxy>();
        _mockContext = new Mock<HubCallerContext>();
        _mockGroups = new Mock<IGroupManager>();
        _mockLogger = new Mock<ILogger<ChatHub>>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();

        _mockDateTimeProvider.Setup(d => d.Now).Returns(_fixedNow);

        _mockClients.Setup(c => c.All).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockClients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(_mockClientProxy.Object);

        _mockContext.Setup(c => c.ConnectionId).Returns("test-conn-id");

        _hub = new ChatHub(_mockLogger.Object, _mockDateTimeProvider.Object)
        {
            Clients = _mockClients.Object,
            Context = _mockContext.Object,
            Groups = _mockGroups.Object
        };
    }

    [Fact]
    public async Task OnConnectedAsync_WithRole_ShouldAddToRoleGroup()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Admin") });
        var principal = new ClaimsPrincipal(identity);
        _mockContext.Setup(c => c.User).Returns(principal);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockGroups.Verify(g => g.AddToGroupAsync("test-conn-id", "role_Admin", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnConnectedAsync_WithoutRole_ShouldNotAddToGroup()
    {
        // Arrange
        _mockContext.Setup(c => c.User).Returns((ClaimsPrincipal?)null);

        // Act
        await _hub.OnConnectedAsync();

        // Assert
        _mockGroups.Verify(g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithException_ShouldCompleteWithoutThrowing()
    {
        // Act & Assert
        await _hub.OnDisconnectedAsync(new InvalidOperationException("Connection lost"));
    }

    [Fact]
    public async Task OnDisconnectedAsync_WithoutException_ShouldCompleteWithoutThrowing()
    {
        // Act & Assert
        await _hub.OnDisconnectedAsync(null);
    }

    [Fact]
    public async Task SendMessage_ShouldBroadcastToRoleGroups()
    {
        // Act
        await _hub.SendMessage("user", "msg");

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveMessage",
                It.Is<object[]>(args => (string)args[0] == "user" && (string)args[1] == "msg" && (DateTime)args[2] == _fixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNewWhatsAppMessage_ShouldBroadcastToRoleGroups()
    {
        // Act
        await _hub.NotifyNewWhatsAppMessage("5551234567", "Hola", "texto");

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "NewWhatsAppMessage",
                It.Is<object[]>(args => (string)args[0] == "5551234567" && (string)args[1] == "Hola" && (string)args[2] == "texto" && (DateTime)args[3] == _fixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyNewMessage_ShouldBroadcastToRoleGroups()
    {
        // Act
        await _hub.NotifyNewMessage("5551234567", "Contenido simplificado");

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "NuevoMensaje",
                It.Is<object[]>(args => (string)args[0] == "5551234567" && (string)args[1] == "Contenido simplificado"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOrderStatusChange_ShouldBroadcastToRoleGroups()
    {
        // Act
        await _hub.NotifyOrderStatusChange("PED-001", "EnRuta");

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "OrderStatusChanged",
                It.Is<object[]>(args => (string)args[0] == "PED-001" && (string)args[1] == "EnRuta" && (DateTime)args[2] == _fixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyOrdersUpdated_ShouldBroadcastToRoleGroups()
    {
        // Act
        await _hub.NotifyOrdersUpdated();

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ActualizarPedidos",
                It.IsAny<object[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifySessionStateChange_ShouldBroadcastToRoleGroups()
    {
        // Act
        await _hub.NotifySessionStateChange("5551234567", "MENU");

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "SessionStateChanged",
                It.Is<object[]>(args => (string)args[0] == "5551234567" && (string)args[1] == "MENU" && (DateTime)args[2] == _fixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinConversation_ShouldAddToGroup()
    {
        // Act
        await _hub.JoinConversation("5551234567");

        // Assert
        _mockGroups.Verify(x => x.AddToGroupAsync("test-conn-id", "5551234567", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LeaveConversation_ShouldRemoveFromGroup()
    {
        // Act
        await _hub.LeaveConversation("5551234567");

        // Assert
        _mockGroups.Verify(x => x.RemoveFromGroupAsync("test-conn-id", "5551234567", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendToConversation_ShouldSendToSpecificGroup()
    {
        // Act
        await _hub.SendToConversation("5551234567", "Mensaje al grupo");

        // Assert
        _mockClients.Verify(c => c.Group("5551234567"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "ReceiveConversationMessage",
                It.Is<object[]>(args => (string)args[0] == "5551234567" && (string)args[1] == "Mensaje al grupo" && (DateTime)args[2] == _fixedNow),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UserTyping_ShouldSendToSpecificGroup()
    {
        // Act
        await _hub.UserTyping("5551234567", true);

        // Assert
        _mockClients.Verify(c => c.Group("5551234567"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync(
                "UserTyping",
                It.Is<object[]>(args => (string)args[0] == "5551234567" && (bool)args[1] == true),
                It.IsAny<CancellationToken>()),
            Times.Once);
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
                 It.Is<object[]>(args => (DateTime)args[0] == _fixedNow),
                 It.IsAny<CancellationToken>()),
             Times.Once);
    }

    [Fact]
    public async Task NotifyActualizarClientes_ShouldBroadcast()
    {
        // Act
        await _hub.NotifyActualizarClientes();

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync("ActualizarClientes", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyActualizarPedidos_ShouldBroadcast()
    {
        // Act
        await _hub.NotifyActualizarPedidos();

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync("ActualizarPedidos", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyActualizarUsuarios_ShouldSendToAdminRoleGroup()
    {
        // Act
        await _hub.NotifyActualizarUsuarios();

        // Assert
        _mockClients.Verify(c => c.Group("role_Admin"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync("ActualizarUsuarios", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyActualizarHome_ShouldBroadcast()
    {
        // Act
        await _hub.NotifyActualizarHome();

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync("ActualizarHome", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyActualizarConversaciones_ShouldBroadcast()
    {
        // Act
        await _hub.NotifyActualizarConversaciones();

        // Assert
        _mockClientProxy.Verify(
            x => x.SendCoreAsync("ActualizarConversaciones", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyActualizarConfiguraciones_ShouldSendToAdminRoleGroup()
    {
        // Act
        await _hub.NotifyActualizarConfiguraciones();

        // Assert
        _mockClients.Verify(c => c.Group("role_Admin"), Times.Once);
        _mockClientProxy.Verify(
            x => x.SendCoreAsync("ActualizarConfiguraciones", It.IsAny<object[]>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
