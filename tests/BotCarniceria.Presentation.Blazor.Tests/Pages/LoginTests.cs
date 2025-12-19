using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Components;
using Moq;
using Xunit;
using MudBlazor;
using MudBlazor.Services;
using FluentAssertions;
using BotCarniceria.Presentation.Blazor.Tests; // Namespace setup

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class LoginTests : IDisposable
{
    private BunitContext Context { get; }

    public LoginTests()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    public void Dispose()
    {
        Context.Dispose();
    }

    [Fact]
    public void Login_ShouldRenderFormWithCorrectAttributes()
    {
        // Act
        var cut = Context.Render<Login>();

        // Assert
        cut.Find("form").GetAttribute("action").Should().Be("/account/login");
        cut.Find("form").GetAttribute("method").Should().Be("post");
        
        // Inputs
        cut.Find("input[name='username']").Should().NotBeNull();
        cut.Find("input[name='password']").Should().NotBeNull();
        cut.Find("input[name='returnUrl']").GetAttribute("value").Should().Be("/");
    }

    [Fact]
    public void Login_WithErrorMessage_ShouldShowAlert()
    {
        // Arrange
        var nav = Context.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/login?error=Credenciales%20invalidas");

        // Act
        var cut = Context.Render<Login>();

        // Assert
        var alert = cut.FindComponent<MudAlert>();
        alert.Instance.Severity.Should().Be(Severity.Error);
        alert.Markup.Should().Contain("Credenciales invalidas"); 
    }
}
