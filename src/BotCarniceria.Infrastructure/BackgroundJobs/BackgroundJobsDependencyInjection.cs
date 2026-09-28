using Hangfire;
using Hangfire.PostgreSql;
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
            .UsePostgreSqlStorage(
                c => c.UseNpgsqlConnection(hangfireOptions.ConnectionString),
                new PostgreSqlStorageOptions
                {
                    SchemaName = hangfireOptions.SchemaName,
                    QueuePollInterval = TimeSpan.FromSeconds(15),
                    PrepareSchemaIfNecessary = true
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
        services.AddScoped<IJobHandler<ProcessIncomingMessageJob>, ProcessIncomingMessageJobHandler>();

        return services;
    }
}
