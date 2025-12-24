using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Infrastructure.BackgroundJobs.Handlers;

public class ProcessIncomingMessageJobHandler : IJobHandler<ProcessIncomingMessageJob>
{
    private readonly IIncomingMessageHandler _incomingMessageHandler;
    private readonly ILogger<ProcessIncomingMessageJobHandler> _logger;

    public ProcessIncomingMessageJobHandler(
        IIncomingMessageHandler incomingMessageHandler,
        ILogger<ProcessIncomingMessageJobHandler> logger)
    {
        _incomingMessageHandler = incomingMessageHandler;
        _logger = logger;
    }

    public async Task ExecuteAsync(ProcessIncomingMessageJob job, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing incoming message ID: {MessageId}", job.Message.Id);
        try
        {
            await _incomingMessageHandler.HandleAsync(job.Message);
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error processing incoming message ID: {MessageId}", job.Message.Id);
             throw;
        }
    }
}
