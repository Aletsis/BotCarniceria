using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Services;

namespace BotCarniceria.Presentation.Blazor.Hubs;

/// <summary>
/// SignalR Hub for real-time communication with Blazor clients
/// Handles notifications for messages, orders, sessions, and dashboard updates
/// </summary>
[Authorize]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ChatHub(ILogger<ChatHub> logger, IDateTimeProvider dateTimeProvider)
    {
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    #region Lifecycle Events

    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(role))
        {
            // Normalizamos el rol y agregamos prefijo para evitar colisiones
            var groupName = $"role_{role}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("Cliente conectado: {ConnectionId} unido al grupo: {GroupName}", Context.ConnectionId, groupName);
        }
        else
        {
            _logger.LogWarning("Cliente conectado sin Rol: {ConnectionId}", Context.ConnectionId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // La limpieza de grupos es automática en SignalR al desconectar
        
        if (exception != null)
        {
            _logger.LogWarning(exception, "Cliente desconectado con error: {ConnectionId}", Context.ConnectionId);
        }
        else
        {
            _logger.LogInformation("Cliente desconectado: {ConnectionId}", Context.ConnectionId);
        }
        
        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    private IReadOnlyList<string> GetAllRoleGroups() => Enum.GetNames(typeof(RolUsuario)).Select(r => $"role_{r}").ToList();

    #region General Messaging

    /// <summary>
    /// Send a general message to all connected clients
    /// </summary>
    public async Task SendMessage(string user, string message)
    {
        await Clients.Groups(GetAllRoleGroups()).SendAsync("ReceiveMessage", user, message, _dateTimeProvider.Now);
    }

    #endregion

    #region WhatsApp Notifications

    /// <summary>
    /// Notify all clients about a new WhatsApp message
    /// </summary>
    public async Task NotifyNewWhatsAppMessage(string phoneNumber, string message, string messageType)
    {
        _logger.LogDebug("Notificando nuevo mensaje de WhatsApp: {PhoneNumber}", phoneNumber);
        await Clients.Groups(GetAllRoleGroups()).SendAsync("NewWhatsAppMessage", phoneNumber, message, messageType, _dateTimeProvider.Now);
    }

    /// <summary>
    /// Notify all clients about a new message (simplified version)
    /// </summary>
    public async Task NotifyNewMessage(string phoneNumber, string content)
    {
        _logger.LogDebug("Notificando nuevo mensaje: {PhoneNumber}", phoneNumber);
        await Clients.Groups(GetAllRoleGroups()).SendAsync("NuevoMensaje", phoneNumber, content);
    }

    #endregion

    #region Order Notifications

    /// <summary>
    /// Notify all clients about an order status change
    /// </summary>
    public async Task NotifyOrderStatusChange(string folio, string newStatus)
    {
        _logger.LogDebug("Notificando cambio de estado de pedido: {Folio} -> {Status}", folio, newStatus);
        await Clients.Groups(GetAllRoleGroups()).SendAsync("OrderStatusChanged", folio, newStatus, _dateTimeProvider.Now);
    }

    /// <summary>
    /// Notify all clients to refresh the orders list
    /// </summary>
    public async Task NotifyOrdersUpdated()
    {
        _logger.LogDebug("Notificando actualización de pedidos");
        await Clients.Groups(GetAllRoleGroups()).SendAsync("ActualizarPedidos");
    }

    #endregion

    #region Session/Conversation Notifications

    /// <summary>
    /// Notify all clients about a session state change
    /// </summary>
    public async Task NotifySessionStateChange(string phoneNumber, string newState)
    {
        _logger.LogDebug("Notificando cambio de estado de sesión: {PhoneNumber} -> {State}", phoneNumber, newState);
        await Clients.Groups(GetAllRoleGroups()).SendAsync("SessionStateChanged", phoneNumber, newState, _dateTimeProvider.Now);
    }

    #endregion

    #region Conversation Groups

    /// <summary>
    /// Join a specific conversation group to receive targeted messages
    /// </summary>
    public async Task JoinConversation(string phoneNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, phoneNumber);
        _logger.LogInformation("Cliente {ConnectionId} unido a conversación {PhoneNumber}", 
            Context.ConnectionId, phoneNumber);
    }

    /// <summary>
    /// Leave a specific conversation group
    /// </summary>
    public async Task LeaveConversation(string phoneNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, phoneNumber);
        _logger.LogInformation("Cliente {ConnectionId} salió de conversación {PhoneNumber}", 
            Context.ConnectionId, phoneNumber);
    }

    /// <summary>
    /// Send a message to a specific conversation group
    /// </summary>
    public async Task SendToConversation(string phoneNumber, string message)
    {
        await Clients.Group(phoneNumber).SendAsync("ReceiveConversationMessage", phoneNumber, message, _dateTimeProvider.Now);
    }

    #endregion

    #region Typing Indicators

    /// <summary>
    /// Notify conversation group members that a user is typing
    /// </summary>
    public async Task UserTyping(string phoneNumber, bool isTyping)
    {
        await Clients.Group(phoneNumber).SendAsync("UserTyping", phoneNumber, isTyping);
    }

    #endregion

    #region Dashboard Notifications

    /// <summary>
    /// Notify all clients to refresh the dashboard
    /// </summary>
    public async Task NotifyDashboardUpdate()
    {
        _logger.LogDebug("Notificando actualización del dashboard");
        await Clients.Groups(GetAllRoleGroups()).SendAsync("DashboardUpdate", _dateTimeProvider.Now);
    }

    #endregion

    #region Page-Specific Notifications

    /// <summary>
    /// Notify all clients to refresh the clients list
    /// </summary>
    public async Task NotifyActualizarClientes()
    {
        _logger.LogDebug("Notificando actualización de clientes");
        await Clients.Groups(GetAllRoleGroups()).SendAsync("ActualizarClientes");
    }

    /// <summary>
    /// Notify all clients to refresh the orders list (alias)
    /// </summary>
    public async Task NotifyActualizarPedidos()
    {
        _logger.LogDebug("Notificando actualización de pedidos");
        await Clients.Groups(GetAllRoleGroups()).SendAsync("ActualizarPedidos");
    }

    /// <summary>
    /// Notify all clients to refresh the users list
    /// </summary>
    public async Task NotifyActualizarUsuarios()
    {
        _logger.LogDebug("Notificando actualización de usuarios");
        await Clients.Group($"role_{RolUsuario.Admin}").SendAsync("ActualizarUsuarios");
    }

    /// <summary>
    /// Notify all clients to refresh the home page
    /// </summary>
    public async Task NotifyActualizarHome()
    {
        _logger.LogDebug("Notificando actualización de home");
        await Clients.Groups(GetAllRoleGroups()).SendAsync("ActualizarHome");
    }

    /// <summary>
    /// Notify all clients to refresh the conversations list
    /// </summary>
    public async Task NotifyActualizarConversaciones()
    {
        _logger.LogDebug("Notificando actualización de conversaciones");
        await Clients.Groups(GetAllRoleGroups()).SendAsync("ActualizarConversaciones");
    }

    /// <summary>
    /// Notify all clients to refresh the configurations
    /// </summary>
    public async Task NotifyActualizarConfiguraciones()
    {
        _logger.LogDebug("Notificando actualización de configuraciones");
        await Clients.Group($"role_{RolUsuario.Admin}").SendAsync("ActualizarConfiguraciones");
    }

    #endregion
}
