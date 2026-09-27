using BotCarniceria.Infrastructure.BackgroundJobs.Configuration;
using BotCarniceria.Presentation.API.Extensions;
using FluentAssertions;
using Hangfire;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Extensions;

public class HangfireServiceExtensionsTests
{
    [Fact]
    public void UseHangfireDashboardWithAuth_WhenDashboardDisabled_ShouldReturnAppWithoutConfiguring()
    {
        // Arrange
        var mockApp = new Mock<IApplicationBuilder>();
        var options = new HangfireOptions
        {
            EnableDashboard = false,
            DashboardPath = "/hangfire"
        };

        // Act
        var result = mockApp.Object.UseHangfireDashboardWithAuth(options);

        // Assert
        result.Should().BeSameAs(mockApp.Object);
        mockApp.Verify(a => a.Use(It.IsAny<Func<Microsoft.AspNetCore.Http.RequestDelegate, Microsoft.AspNetCore.Http.RequestDelegate>>()), Times.Never);
    }

    [Fact]
    public void UseHangfireDashboardWithAuth_WhenDashboardEnabled_ShouldConfigureDashboardAndReturnApp()
    {
        // Arrange
        var mockApp = new Mock<IApplicationBuilder>();
        var mockJobStorage = new Mock<JobStorage>();
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddHangfire(cfg => { });
        services.AddSingleton(mockJobStorage.Object);
        var serviceProvider = services.BuildServiceProvider();

        mockApp.Setup(a => a.ApplicationServices).Returns(serviceProvider);
        mockApp.Setup(a => a.New()).Returns(mockApp.Object);
        mockApp.Setup(a => a.Properties).Returns(new Dictionary<string, object?>());

        var options = new HangfireOptions
        {
            EnableDashboard = true,
            DashboardPath = "/hangfire-test"
        };

        // Act
        var result = mockApp.Object.UseHangfireDashboardWithAuth(options);

        // Assert
        result.Should().BeSameAs(mockApp.Object);
    }
}
