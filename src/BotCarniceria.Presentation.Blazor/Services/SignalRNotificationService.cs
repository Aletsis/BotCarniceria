using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Presentation.Blazor.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace BotCarniceria.Presentation.Blazor.Services;

public class SignalRNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<ChatHub> _hubContext;

    public SignalRNotificationService(IHubContext<ChatHub> hubContext)
    {
        _hubContext = hubContext;
    }
    
    // Dashboard roles to notify
    private static readonly IReadOnlyList<string> DashboardGroups = new[] 
    { 
        $"role_{BotCarniceria.Core.Domain.Enums.RolUsuario.Admin}", 
        $"role_{BotCarniceria.Core.Domain.Enums.RolUsuario.Supervisor}", 
        $"role_{BotCarniceria.Core.Domain.Enums.RolUsuario.Editor}", 
        $"role_{BotCarniceria.Core.Domain.Enums.RolUsuario.Viewer}" 
    };

    public async Task NotifyNewMessageAsync(string phoneNumber, string message)
    {
        // Notify dashboard users
        await _hubContext.Clients.Groups(DashboardGroups).SendAsync("ReceiveMessage", phoneNumber, message, DateTime.Now);
    }

    public async Task NotifyOrdersUpdatedAsync()
    {
        await _hubContext.Clients.Groups(DashboardGroups).SendAsync("ActualizarPedidos");
    }

    public async Task NotifySessionExpiredAsync(string phoneNumber)
    {
        // Notify dashboard (maybe specific event or just session state change)
        await _hubContext.Clients.Groups(DashboardGroups).SendAsync("SessionStateChanged", phoneNumber, "EXPIRED", DateTime.Now);
    }

    public async Task NotifyOrderPrintedAsync(string folio)
    {
        await _hubContext.Clients.Groups(DashboardGroups).SendAsync("OrderStatusChanged", folio, "Impreso", DateTime.Now);
    }

    public async Task NotifyUserTypingAsync(string phoneNumber, bool isTyping)
    {
        // This notifies anyone listening to that specific phone number conversation (e.g. valid admin in chat view)
        await _hubContext.Clients.Group(phoneNumber).SendAsync("UserTyping", phoneNumber, isTyping);
    }

    public async Task NotifyOrderPickedUpAsync(string folio)
    {
        await _hubContext.Clients.Groups(DashboardGroups).SendAsync("OrderStatusChanged", folio, "Entregado", DateTime.Now);
    }
}
