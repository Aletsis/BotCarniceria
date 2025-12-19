using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.RateLimiting;

namespace BotCarniceria.Presentation.API.Controllers;

[ApiController]
[Route("api/webhook")]
[EnableRateLimiting("WebhookPolicy")]
public class WebhookController : ControllerBase
{
    private readonly IIncomingMessageHandler _incomingMessageHandler;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IIncomingMessageHandler incomingMessageHandler,
        IUnitOfWork unitOfWork,
        ILogger<WebhookController> logger)
    {
        _incomingMessageHandler = incomingMessageHandler;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> VerifyToken()
    {
        var token = await _unitOfWork.Settings.GetValorAsync("WhatsApp_VerifyToken");
        var mode = Request.Query["hub.mode"];
        var verifyToken = Request.Query["hub.verify_token"];
        var challenge = Request.Query["hub.challenge"];

        if (!string.IsNullOrEmpty(mode) && !string.IsNullOrEmpty(verifyToken))
        {
            if (mode == "subscribe" && verifyToken == token)
            {
                _logger.LogInformation("Webhook verified successfully");
                return Content(challenge, "text/plain");
            }
        }

        return Forbid();
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveMessage([FromBody] WebhookPayload payload)
    {
        try
        {
            if (payload?.Entry == null) return BadRequest();

            foreach (var entry in payload.Entry)
            {
                if (entry.Changes == null) continue;

                foreach (var change in entry.Changes)
                {
                    if (change.Value?.Messages == null) continue;

                    foreach (var message in change.Value.Messages)
                    {
                        // Fire and forget or await? 
                        // Usually webhooks need quick 200 OK.
                        // But we want to ensure processing.
                        // I'll await for now, or we can use background queue.
                        await _incomingMessageHandler.HandleAsync(message);
                    }
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return StatusCode(500);
        }
    }
}
