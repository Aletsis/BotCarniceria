using Hangfire;
using Hangfire.SqlServer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Infrastructure.BackgroundJobs.Configuration;
using BotCarniceria.Infrastructure.BackgroundJobs.Services;
using BotCarniceria.Infrastructure.BackgroundJobs.Handlers;

namespace BotCarniceria.Infrastructure.BackgroundJobs;

/// <summary>
/// Extensiones para configurar Hangfire y trabajos en segundo plano
/// </summary>
public static class BackgroundJobsDependencyInjection
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configuración
        var hangfireOptions = configuration
            .GetSection(HangfireOptions.SectionName)
            .Get<HangfireOptions>() ?? new HangfireOptions();

        services.Configure<HangfireOptions>(
            configuration.GetSection(HangfireOptions.SectionName));

        // Hangfire
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(
                hangfireOptions.ConnectionString,
                new SqlServerStorageOptions
                {
                    SchemaName = hangfireOptions.SchemaName,
                    CommandBatchMaxTimeout = TimeSpan.FromMinutes(5),
                    SlidingInvisibilityTimeout = TimeSpan.FromMinutes(5),
                    QueuePollInterval = TimeSpan.Zero,
                    UseRecommendedIsolationLevel = true,
                    DisableGlobalLocks = true
                }));

        services.AddHangfireServer(options =>
        {
            options.WorkerCount = hangfireOptions.WorkerCount;
        });

        // Servicios
        services.AddScoped<IBackgroundJobService, HangfireBackgroundJobService>();

        // Handlers
        services.AddScoped<IJobHandler<EnqueueWhatsAppMessageJob>, WhatsAppJobHandler>();
        services.AddScoped<IJobHandler<EnqueuePrintJob>, PrintJobHandler>();

        return services;
    }
}
