using BotCarniceria.Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BotCarniceria.Presentation.Blazor.Controllers;

[ApiController]
[Route("api/internal/notifications")]
public class InternalNotificationController : ControllerBase
{
    private readonly IRealTimeNotificationService _notificationService;

    public InternalNotificationController(IRealTimeNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpPost("message")]
    public async Task<IActionResult> NotifyNewMessage([FromBody] NewMessageNotificationRequest request)
    {
        await _notificationService.NotifyNewMessageAsync(request.PhoneNumber, request.Message);
        return Ok();
    }
}

public class NewMessageNotificationRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
