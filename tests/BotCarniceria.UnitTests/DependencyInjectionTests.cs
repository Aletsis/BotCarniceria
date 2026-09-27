using BotCarniceria.Core;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BotCarniceria.UnitTests;

public class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_RegistersMediatRServicesAndReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddApplication();

        // Assert
        result.Should().BeSameAs(services);
        services.Should().Contain(s => s.ServiceType == typeof(IMediator));
        services.Should().Contain(s => s.ServiceType == typeof(ISender));
        services.Should().Contain(s => s.ServiceType == typeof(IPublisher));
    }

    [Fact]
    public void AddApplication_CanBuildProviderAndResolveMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddApplication();

        // Act
        using var provider = services.BuildServiceProvider();
        var mediator = provider.GetService<IMediator>();
        var sender = provider.GetService<ISender>();
        var publisher = provider.GetService<IPublisher>();

        // Assert
        mediator.Should().NotBeNull();
        sender.Should().NotBeNull();
        publisher.Should().NotBeNull();
    }
}
