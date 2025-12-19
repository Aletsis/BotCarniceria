using BotCarniceria.Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BotCarniceria.Presentation.API.Controllers;

[ApiController]
[Route("api/whatsapp-test")]
public class WhatsAppTestController : ControllerBase
{
    private readonly IWhatsAppService _whatsAppService;

    public WhatsAppTestController(IWhatsAppService whatsAppService)
    {
        _whatsAppService = whatsAppService;
    }

    [HttpPost("send-text")]
    public async Task<IActionResult> SendText([FromQuery] string phoneNumber, [FromBody] string message)
    {
        if (string.IsNullOrEmpty(phoneNumber) || string.IsNullOrEmpty(message))
        {
            return BadRequest("Phone number and message are required.");
        }

        var result = await _whatsAppService.SendTextMessageAsync(phoneNumber, message);

        if (result)
        {
            return Ok("Message sent successfully.");
        }

        return StatusCode(500, "Failed to send message. Check logs for details.");
    }
}
