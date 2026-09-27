using Bunit;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Components;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor.Services;
using System.Security.Claims;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Components;

public class RoutesTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;
    private readonly Mock<IAuthorizationService> _mockAuthService;

    public RoutesTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
        _mockAuthService = new Mock<IAuthorizationService>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddAuthorizationCore();

        // Default mock mediator for MainLayout or pages
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConfiguracionesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ConfiguracionDto>());
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllSolicitudesFacturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SolicitudFacturaDto>());

        Context.Services.AddSingleton(_mockMediator.Object);
        Context.Services.AddSingleton(_mockAuthStateProvider.Object);
        Context.Services.AddSingleton(_mockAuthService.Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void Routes_WhenUnauthenticated_AndNavigatingToProtectedRoute_ShouldRedirectToLogin()
    {
        // Arrange - Unauthenticated user
        var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(anonymousPrincipal);
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/facturas");

        // Act
        var cut = Context.Render<Routes>();

        // Assert - Navigation should have redirected to "login"
        nav.Uri.Should().EndWith("login");
    }

    [Fact]
    public void Routes_WhenAuthenticatedWithoutRequiredRole_ShouldShowNoPermissionsAlert()
    {
        // Arrange - Authenticated but without required role
        var claims = new List<Claim> { new Claim(ClaimTypes.Name, "UserWithoutRoles") };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Failed());

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/facturas");

        // Act
        var cut = Context.Render<Routes>();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("No tienes permisos para acceder a esta página."));
    }

    [Fact]
    public void Routes_WhenNavigatingToPublicPage_ShouldRenderPublicContent()
    {
        // Arrange - Unauthenticated user visiting public page
        var anonymousPrincipal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(anonymousPrincipal);
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());

        var nav = Context.Services.GetRequiredService<NavigationManager>();
        nav.NavigateTo("/solicitar-factura");

        // Act
        var cut = Context.Render<Routes>();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Solicitud de Factura"));
    }
}
