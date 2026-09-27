using System.Text;
using BotCarniceria.Presentation.API.Extensions;
using FluentAssertions;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Extensions;

public class HangfireAuthorizationFilterTests
{
    private readonly HangfireAuthorizationFilter _filter;
    private readonly Mock<JobStorage> _mockJobStorage;

    public HangfireAuthorizationFilterTests()
    {
        _filter = new HangfireAuthorizationFilter();
        _mockJobStorage = new Mock<JobStorage>();
    }

    private AspNetCoreDashboardContext CreateContext(HttpContext httpContext)
    {
        httpContext.RequestServices ??= new Mock<IServiceProvider>().Object;
        return new AspNetCoreDashboardContext(_mockJobStorage.Object, new DashboardOptions(), httpContext);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("127.0.0.1")]
    public void Authorize_WhenLocalhost_ShouldReturnTrue(string host)
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString(host);
        var context = CreateContext(httpContext);

        // Act
        var result = _filter.Authorize(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Authorize_RemoteHost_WithoutAuthorizationHeader_ShouldSet401AndReturnFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("api.botcarniceria.com");
        var context = CreateContext(httpContext);

        // Act
        var result = _filter.Authorize(context);

        // Assert
        result.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
        httpContext.Response.Headers["WWW-Authenticate"].ToString().Should().Contain("Basic realm=");
    }

    [Fact]
    public void Authorize_RemoteHost_WithNonBasicAuthScheme_ShouldSet401AndReturnFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("api.botcarniceria.com");
        httpContext.Request.Headers["Authorization"] = "Bearer some_jwt_token";
        var context = CreateContext(httpContext);

        // Act
        var result = _filter.Authorize(context);

        // Assert
        result.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Authorize_RemoteHost_WithInvalidCredentials_ShouldSet401AndReturnFalse()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("api.botcarniceria.com");
        var invalidCreds = Convert.ToBase64String(Encoding.UTF8.GetBytes("wronguser:wrongpass"));
        httpContext.Request.Headers["Authorization"] = $"Basic {invalidCreds}";
        var context = CreateContext(httpContext);

        // Act
        var result = _filter.Authorize(context);

        // Assert
        result.Should().BeFalse();
        httpContext.Response.StatusCode.Should().Be(401);
    }

    [Fact]
    public void Authorize_RemoteHost_WithDefaultCredentials_ShouldReturnTrue()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Host = new HostString("api.botcarniceria.com");
        var defaultCreds = Convert.ToBase64String(Encoding.UTF8.GetBytes("admin:admin"));
        httpContext.Request.Headers["Authorization"] = $"Basic {defaultCreds}";
        var context = CreateContext(httpContext);

        // Act
        var result = _filter.Authorize(context);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Authorize_RemoteHost_WithCustomConfigCredentials_ShouldReturnTrue()
    {
        // Arrange
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "Hangfire:User", "customAdmin" },
            { "Hangfire:Pass", "Secret123!" }
        };

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        var mockServiceProvider = new Mock<IServiceProvider>();
        mockServiceProvider.Setup(sp => sp.GetService(typeof(IConfiguration)))
            .Returns(configuration);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = mockServiceProvider.Object;
        httpContext.Request.Host = new HostString("api.botcarniceria.com");

        var customCreds = Convert.ToBase64String(Encoding.UTF8.GetBytes("customAdmin:Secret123!"));
        httpContext.Request.Headers["Authorization"] = $"Basic {customCreds}";
        var context = CreateContext(httpContext);

        // Act
        var result = _filter.Authorize(context);

        // Assert
        result.Should().BeTrue();
    }
}
