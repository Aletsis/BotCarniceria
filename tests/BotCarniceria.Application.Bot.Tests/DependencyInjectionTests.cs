using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.StateMachine.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace BotCarniceria.Application.Bot.Tests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddBotApplication_ShouldRegisterServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddBotApplication();

        // Assert
        // Verify Interaces
        services.Should().Contain(d => d.ServiceType == typeof(IIncomingMessageHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(IStateHandlerFactory) && d.Lifetime == ServiceLifetime.Scoped);

        // Verify Concrete Handlers
        services.Should().Contain(d => d.ServiceType == typeof(StartStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(MenuStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(AskNameStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(AskAddressStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(TakingOrderStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(AddingMoreStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(AwaitingConfirmStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(ConfirmAddressStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(d => d.ServiceType == typeof(SelectPaymentStateHandler) && d.Lifetime == ServiceLifetime.Scoped);
    }
}
