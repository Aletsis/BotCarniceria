using Hangfire;
using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;

namespace BotCarniceria.Infrastructure.BackgroundJobs.Services;

/// <summary>
/// Implementación de IBackgroundJobService usando Hangfire
/// </summary>
public class HangfireBackgroundJobService : IBackgroundJobService
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly ILogger<HangfireBackgroundJobService> _logger;

    public HangfireBackgroundJobService(
        IBackgroundJobClient backgroundJobClient,
        IRecurringJobManager recurringJobManager,
        ILogger<HangfireBackgroundJobService> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _recurringJobManager = recurringJobManager;
        _logger = logger;
    }

    public Task<string> EnqueueAsync<TJob>(TJob job, CancellationToken cancellationToken = default)
        where TJob : class, IJob
    {
        try
        {
            var jobId = _backgroundJobClient.Enqueue<IJobHandler<TJob>>(
                handler => handler.ExecuteAsync(job, cancellationToken));

            _logger.LogInformation(
                "Job enqueued: {JobType} with ID: {JobId}",
                typeof(TJob).Name,
                jobId);

            return Task.FromResult(jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enqueue job: {JobType}", typeof(TJob).Name);
            throw;
        }
    }

    public Task<string> ScheduleAsync<TJob>(TJob job, TimeSpan delay, CancellationToken cancellationToken = default)
        where TJob : class, IJob
    {
        try
        {
            var jobId = _backgroundJobClient.Schedule<IJobHandler<TJob>>(
                handler => handler.ExecuteAsync(job, cancellationToken),
                delay);

            _logger.LogInformation(
                "Job scheduled: {JobType} with ID: {JobId} for {Delay}",
                typeof(TJob).Name,
                jobId,
                delay);

            return Task.FromResult(jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to schedule job: {JobType}", typeof(TJob).Name);
            throw;
        }
    }

    public Task AddRecurringJobAsync<TJob>(string jobId, TJob job, string cronExpression, CancellationToken cancellationToken = default)
        where TJob : class, IJob
    {
        try
        {
            _recurringJobManager.AddOrUpdate<IJobHandler<TJob>>(
                jobId,
                handler => handler.ExecuteAsync(job, cancellationToken),
                cronExpression);

            _logger.LogInformation(
                "Recurring job added: {JobType} with ID: {JobId} (Cron: {Cron})",
                typeof(TJob).Name,
                jobId,
                cronExpression);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add recurring job: {JobType}", typeof(TJob).Name);
            throw;
        }
    }

    public Task<bool> DeleteJobAsync(string jobId, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = _backgroundJobClient.Delete(jobId);
            _logger.LogInformation("Job deleted: {JobId} - Success: {Result}", jobId, result);
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete job: {JobId}", jobId);
            throw;
        }
    }
}
