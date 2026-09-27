using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Shared;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Security.Claims;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Components;

public class SessionTimeoutTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<IDialogService> _mockDialogService;

    public SessionTimeoutTests()
    {
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        _mockDialogService = new Mock<IDialogService>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddAuthorizationCore();
        Context.Services.AddSingleton(_mockAuthStateProvider.Object);
        Context.Services.AddSingleton(_mockDialogService.Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void SessionTimeout_WhenAuthenticated_ShouldCallInitSessionTimeout()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Admin") }, "TestAuth"));
        var authState = new AuthenticationState(principal);
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);

        // Act
        var cut = Context.Render<SessionTimeout>(p =>
        {
            p.Add(x => x.TimeoutMinutes, 30);
            p.Add(x => x.WarningMinutes, 5);
        });

        // Assert - JSInterop in Loose mode handles invocation
        var invocations = Context.JSInterop.Invocations["sessionTimeout.initialize"];
        invocations.Should().NotBeEmpty();
    }

    [Fact]
    public void SessionTimeout_WhenNotAuthenticated_ShouldNotCallInit()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(principal);
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);

        // Act
        var cut = Context.Render<SessionTimeout>();

        // Assert
        var invocations = Context.JSInterop.Invocations.Where(i => i.Identifier == "sessionTimeout.initialize");
        invocations.Should().BeEmpty();
    }

    [Fact]
    public void SessionTimeout_Logout_ShouldNavigateToAccountLogout()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(principal));

        var cut = Context.Render<SessionTimeout>();
        var nav = Context.Services.GetRequiredService<NavigationManager>();

        // Act
        cut.Instance.Logout();

        // Assert
        nav.Uri.Should().EndWith("/account/logout");
    }

    [Fact]
    public async Task SessionTimeout_ShowSessionWarning_WhenUserConfirms_ShouldKeepAlive()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Admin") }, "TestAuth"));
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(principal));

        _mockDialogService.Setup(d => d.ShowMessageBox(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DialogOptions>()))
            .ReturnsAsync(true);

        var cut = Context.Render<SessionTimeout>();

        // Act
        await cut.Instance.ShowSessionWarning();

        // Assert
        var fetchInvocations = Context.JSInterop.Invocations.Where(i => i.Identifier == "fetch");
        fetchInvocations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task SessionTimeout_ShowSessionWarning_WhenUserCancels_ShouldLogout()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Admin") }, "TestAuth"));
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(principal));

        _mockDialogService.Setup(d => d.ShowMessageBox(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DialogOptions>()))
            .ReturnsAsync(false);

        var cut = Context.Render<SessionTimeout>();
        var nav = Context.Services.GetRequiredService<NavigationManager>();

        // Act
        await cut.Instance.ShowSessionWarning();

        // Assert
        nav.Uri.Should().EndWith("/account/logout");
    }

    [Fact]
    public void SessionTimeout_Dispose_WhenInitialized_ShouldCallSessionTimeoutDispose()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "Admin") }, "TestAuth"));
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(new AuthenticationState(principal));

        var cut = Context.Render<SessionTimeout>();

        // Act
        cut.Instance.Dispose();

        // Assert
        var disposeInvocations = Context.JSInterop.Invocations.Where(i => i.Identifier == "sessionTimeout.dispose");
        disposeInvocations.Should().NotBeEmpty();
    }
}
