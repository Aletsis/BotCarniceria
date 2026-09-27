using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Services;
using BotCarniceria.Infrastructure;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using BotCarniceria.Infrastructure.Resilience;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterAllRequiredServices()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=.;Database=TestBot;Integrated Security=true;TrustServerCertificate=true;" },
            { "Hangfire:ConnectionString", "Server=.;Database=TestHangfire;Integrated Security=true;TrustServerCertificate=true;" },
            { "WhatsAppCircuitBreaker:FailureRateThreshold", "50" }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

        // Act
        var result = services.AddInfrastructure(configuration);

        // Assert
        result.Should().BeSameAs(services);

        // Repositories
        services.Should().Contain(d => d.ServiceType == typeof(IOrderRepository) && d.ImplementationType == typeof(OrderRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IClienteRepository) && d.ImplementationType == typeof(ClienteRepository));
        services.Should().Contain(d => d.ServiceType == typeof(ISessionRepository) && d.ImplementationType == typeof(SessionRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IMessageRepository) && d.ImplementationType == typeof(MessageRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IConfiguracionRepository) && d.ImplementationType == typeof(ConfiguracionRepository));
        services.Should().Contain(d => d.ServiceType == typeof(IUsuarioRepository) && d.ImplementationType == typeof(UsuarioRepository));
        services.Should().Contain(d => d.ServiceType == typeof(ISolicitudFacturaRepository) && d.ImplementationType == typeof(SolicitudFacturaRepository));

        // Unit of Work
        services.Should().Contain(d => d.ServiceType == typeof(IUnitOfWork));

        // Core infrastructure services
        services.Should().Contain(d => d.ServiceType == typeof(ICacheService));
        services.Should().Contain(d => d.ServiceType == typeof(IPrintingService));
        services.Should().Contain(d => d.ServiceType == typeof(IPasswordHasher));
        services.Should().Contain(d => d.ServiceType == typeof(IDateTimeProvider));
        services.Should().Contain(d => d.ServiceType == typeof(IResilienceMetricsCollector));
        services.Should().Contain(d => d.ServiceType == typeof(IWhatsAppService));
    }
}
