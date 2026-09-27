using BotCarniceria.Presentation.API.Extensions;
using FluentAssertions;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Extensions;

public class CorsServiceExtensionsTests
{
    [Fact]
    public void GetPolicyName_ShouldReturnRestrictedCorsPolicy()
    {
        CorsServiceExtensions.GetPolicyName().Should().Be("RestrictedCorsPolicy");
    }

    [Fact]
    public void AddRestrictedCors_WithConfiguredOrigins_ShouldSetOriginsAndCredentials()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Cors:AllowedOrigins:0", "https://app.botcarniceria.com" },
            { "Cors:AllowedOrigins:1", "https://admin.botcarniceria.com" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

        // Act
        var resultServices = services.AddRestrictedCors(configuration);

        // Assert
        resultServices.Should().BeSameAs(services);

        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;

        var policy = corsOptions.GetPolicy(CorsServiceExtensions.GetPolicyName());
        policy.Should().NotBeNull();
        policy!.Origins.Should().Contain(new[] { "https://app.botcarniceria.com", "https://admin.botcarniceria.com" });
        policy.SupportsCredentials.Should().BeTrue();
        policy.AllowAnyMethod.Should().BeTrue();
        policy.AllowAnyHeader.Should().BeTrue();
    }

    [Fact]
    public void AddRestrictedCors_WithoutOrigins_ShouldAllowAnyOriginWithoutCredentials()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>();

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var services = new ServiceCollection();

        // Act
        services.AddRestrictedCors(configuration);

        // Assert
        var sp = services.BuildServiceProvider();
        var corsOptions = sp.GetRequiredService<IOptions<CorsOptions>>().Value;

        var policy = corsOptions.GetPolicy(CorsServiceExtensions.GetPolicyName());
        policy.Should().NotBeNull();
        policy!.AllowAnyOrigin.Should().BeTrue();
        policy.SupportsCredentials.Should().BeFalse();
        policy.AllowAnyMethod.Should().BeTrue();
        policy.AllowAnyHeader.Should().BeTrue();
    }
}
