using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MudBlazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class ErrorTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices(); // Just in case
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void Error_ShouldShowRequestId_FromHttpContext()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "TEST-TRACE-ID-999";

        // Act
        var cut = Context.Render<Error>(parameters => 
            parameters.AddCascadingValue(httpContext)
        );

        // Assert
        cut.Markup.Should().Contain("TEST-TRACE-ID-999");
    }
}
