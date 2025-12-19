using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Enums;
using MediatR;

namespace BotCarniceria.Presentation.API.Services;

public class SessionTimeoutBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SessionTimeoutBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);
    private const int WarningMinutesBefore = 2;

    public SessionTimeoutBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<SessionTimeoutBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Session Timeout Background Service starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckSessionsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for session timeouts.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Session Timeout Background Service stopping.");
    }

    private async Task CheckSessionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        await mediator.Send(new BotCarniceria.Core.Application.CQRS.Commands.CheckSessionTimeoutsCommand(), stoppingToken);
    }
}
