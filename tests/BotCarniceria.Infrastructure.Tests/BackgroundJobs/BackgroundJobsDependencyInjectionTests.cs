using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Infrastructure.BackgroundJobs;
using BotCarniceria.Infrastructure.BackgroundJobs.Configuration;
using BotCarniceria.Infrastructure.BackgroundJobs.Handlers;
using BotCarniceria.Infrastructure.BackgroundJobs.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.BackgroundJobs;

public class BackgroundJobsDependencyInjectionTests
{
    [Fact]
    public void AddBackgroundJobs_ShouldRegisterAllRequiredServicesAndHandlers()
    {
        // Arrange
        var inMemory = new Dictionary<string, string?>
        {
            { "Hangfire:ConnectionString", "Server=.;Database=TestHangfire;Integrated Security=true;TrustServerCertificate=true;" },
            { "Hangfire:SchemaName", "custom_hangfire" },
            { "Hangfire:WorkerCount", "4" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemory)
            .Build();

        var services = new ServiceCollection();

        // Act
        var result = services.AddBackgroundJobs(configuration);

        // Assert
        result.Should().BeSameAs(services);

        // Check options registration
        services.Should().Contain(d => d.ServiceType == typeof(IConfigureOptions<HangfireOptions>));

        // Check background job service
        services.Should().Contain(d =>
            d.ServiceType == typeof(IBackgroundJobService) &&
            d.ImplementationType == typeof(HangfireBackgroundJobService) &&
            d.Lifetime == ServiceLifetime.Scoped);

        // Check handlers
        services.Should().Contain(d =>
            d.ServiceType == typeof(IJobHandler<EnqueueWhatsAppMessageJob>) &&
            d.ImplementationType == typeof(WhatsAppJobHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IJobHandler<EnqueuePrintJob>) &&
            d.ImplementationType == typeof(PrintJobHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IJobHandler<ProcessIncomingMessageJob>) &&
            d.ImplementationType == typeof(ProcessIncomingMessageJobHandler) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }
}
