using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Components;

public class LoadingOverlayTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void LoadingOverlay_WhenNotVisible_ShouldRenderNothing()
    {
        // Act
        var cut = Context.Render<LoadingOverlay>(p =>
        {
            p.Add(x => x.Visible, false);
            p.Add(x => x.Text, "Cargando...");
        });

        // Assert
        cut.FindAll(".loading-overlay").Should().BeEmpty();
        cut.FindAll(".mud-progress-circular").Should().BeEmpty();
    }

    [Fact]
    public void LoadingOverlay_WhenVisible_ShouldRenderOverlayAndCircularProgress()
    {
        // Act
        var cut = Context.Render<LoadingOverlay>(p =>
        {
            p.Add(x => x.Visible, true);
        });

        // Assert
        cut.Find(".loading-overlay").Should().NotBeNull();
        cut.Find(".mud-progress-circular").Should().NotBeNull();
    }

    [Fact]
    public void LoadingOverlay_WhenVisibleWithText_ShouldRenderText()
    {
        // Act
        var cut = Context.Render<LoadingOverlay>(p =>
        {
            p.Add(x => x.Visible, true);
            p.Add(x => x.Text, "Procesando solicitud...");
        });

        // Assert
        cut.Markup.Should().Contain("Procesando solicitud...");
    }
}
