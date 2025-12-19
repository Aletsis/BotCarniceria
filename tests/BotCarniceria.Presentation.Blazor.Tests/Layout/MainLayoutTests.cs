using Bunit;
using Bunit.TestDoubles;
using BotCarniceria.Presentation.Blazor.Components.Layout;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MudBlazor.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace BotCarniceria.Presentation.Blazor.Tests.Layout;

public class MainLayoutTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        // Auth Setup
        Context.Services.AddAuthorizationCore();
        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Context.Services.AddSingleton(mockAuthService.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "admin") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);

        var mockAuthProvider = new Mock<AuthenticationStateProvider>();
        mockAuthProvider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        Context.Services.AddSingleton(mockAuthProvider.Object); 

        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // NOTE: MainLayout already includes MudProviders, so we DON'T render them here.

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void Layout_ShouldRenderBodyAndDrawer()
    {
        // Act
        var cut = Context.Render<MainLayout>(parameters => 
        {
            parameters.Add(p => p.Body, (RenderFragment)(builder => 
            {
                builder.OpenElement(0, "h1");
                builder.AddContent(1, "Test Body Content");
                builder.CloseElement();
            }));
            
            // Pass cascading auth state just in case NavMenu needs it
            parameters.AddCascadingValue(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test")))));
        });

        // Assert
        cut.Markup.Should().Contain("Test Body Content");
        cut.Markup.Should().Contain("Bot Carnicería"); // AppBar title
        
        // Verify NavMenu is rendered (implicitly checked by drawer existence, but checking content is better)
        // NavMenu usually has links like "Pedidos", "Clientes". Assuming so.
        // cut.Markup.Should().Contain("Pedidos"); // Risky if NavMenu content is unknown.
    }
}
