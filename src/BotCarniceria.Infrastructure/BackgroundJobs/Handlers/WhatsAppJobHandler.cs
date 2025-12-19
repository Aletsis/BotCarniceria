using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using Hangfire;

namespace BotCarniceria.Infrastructure.BackgroundJobs.Handlers;

/// <summary>
/// Handler para trabajos de envío de mensajes de WhatsApp
/// </summary>
public class WhatsAppJobHandler : IJobHandler<EnqueueWhatsAppMessageJob>
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<WhatsAppJobHandler> _logger;

    public WhatsAppJobHandler(
        IWhatsAppService whatsAppService,
        ILogger<WhatsAppJobHandler> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta el trabajo de envío de mensaje de WhatsApp con reintentos exponenciales
    /// </summary>
    [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 10, 30, 60, 300, 900 })]
    public async Task ExecuteAsync(EnqueueWhatsAppMessageJob job, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Executing WhatsApp job {JobId} for phone {PhoneNumber}",
                job.JobId,
                job.PhoneNumber);

            var success = await _whatsAppService.SendTextMessageAsync(
                job.PhoneNumber,
                job.Message);

            if (!success)
            {
                throw new InvalidOperationException(
                    $"Failed to send WhatsApp message to {job.PhoneNumber}");
            }

            _logger.LogInformation(
                "WhatsApp job {JobId} completed successfully",
                job.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "WhatsApp job {JobId} failed for phone {PhoneNumber}. Attempt will be retried by Hangfire.",
                job.JobId,
                job.PhoneNumber);
            throw; // Hangfire manejará el reintento
        }
    }
}
