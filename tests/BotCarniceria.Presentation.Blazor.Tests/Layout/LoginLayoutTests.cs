using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Layout;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Layout;

public class LoginLayoutTests : IAsyncLifetime
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
    public void LoginLayout_ShouldRenderBodyContent()
    {
        // Act
        var cut = Context.Render<LoginLayout>(parameters =>
        {
            parameters.Add(p => p.Body, (RenderFragment)(builder =>
            {
                builder.OpenElement(0, "div");
                builder.AddAttribute(1, "id", "login-body-test");
                builder.AddContent(2, "Login Page Content");
                builder.CloseElement();
            }));
        });

        // Assert
        cut.Markup.Should().Contain("login-body-test");
        cut.Markup.Should().Contain("Login Page Content");
        cut.Find(".mud-main-content").Should().NotBeNull();
    }
}
