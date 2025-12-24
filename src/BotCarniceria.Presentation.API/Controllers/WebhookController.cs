using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BotCarniceria.Presentation.API.Controllers;

[ApiController]
[Route("api/webhook")]
[EnableRateLimiting("WebhookPolicy")]
public class WebhookController : ControllerBase
{
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(
        IBackgroundJobService backgroundJobService,
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        ILogger<WebhookController> logger)
    {
        _backgroundJobService = backgroundJobService;
        _cacheService = cacheService;
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
                        if (string.IsNullOrEmpty(message.Id)) continue;

                        var cacheKey = $"webhook_msg_{message.Id}";
                        
                        // Idempotency check: process only if not already processed
                        if (await _cacheService.ExistsAsync(cacheKey))
                        {
                            _logger.LogInformation("Message {MessageId} duplicate detected, skipping.", message.Id);
                            continue;
                        }

                        // Mark as processed (using a dummy string value since T must be class)
                        await _cacheService.SetAsync(cacheKey, "processed", TimeSpan.FromHours(24));

                        // Queue processing
                        await _backgroundJobService.EnqueueAsync(new ProcessIncomingMessageJob { Message = message });
                        
                        _logger.LogInformation("Message {MessageId} enqueued for processing.", message.Id);
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
