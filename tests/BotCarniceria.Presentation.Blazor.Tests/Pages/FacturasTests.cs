using Bunit;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Security.Claims;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class FacturasTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public FacturasTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockSnackbar = new Mock<ISnackbar>();
        _mockDialogService = new Mock<IDialogService>();
        _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddAuthorizationCore();

        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync((ClaimsPrincipal user, object? resource, IEnumerable<IAuthorizationRequirement> reqs) =>
                (user.IsInRole("admin") || user.IsInRole("supervisor"))
                    ? AuthorizationResult.Success()
                    : AuthorizationResult.Failed());
        Context.Services.AddSingleton(mockAuthService.Object);

        Context.Services.AddSingleton(_mockMediator.Object);
        Context.Services.AddSingleton(_mockSnackbar.Object);
        Context.Services.AddSingleton(_mockDialogService.Object);
        Context.Services.AddSingleton(_mockAuthStateProvider.Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    private AuthenticationState CreateAuthState(string role)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        return new AuthenticationState(principal);
    }

    private List<SolicitudFacturaDto> CreateSampleSolicitudes()
    {
        return new List<SolicitudFacturaDto>
        {
            new SolicitudFacturaDto
            {
                SolicitudFacturaID = 101,
                ClienteNombre = "Juan Perez",
                ClienteTelefono = "5512345678",
                Folio = "T-001",
                Total = 1500.50m,
                RazonSocial = "Juan Perez SA",
                RFC = "JUPE800101XYZ",
                RegimenFiscal = "612",
                RegimenFiscalDescripcion = "Personas Físicas",
                UsoCFDI = "G03",
                UsoCFDIDescripcion = "Gastos en general",
                Estado = "Pendiente",
                FechaSolicitud = DateTime.UtcNow
            },
            new SolicitudFacturaDto
            {
                SolicitudFacturaID = 102,
                ClienteNombre = "Maria Lopez",
                ClienteTelefono = "5587654321",
                Folio = "T-002",
                Total = 2300.00m,
                RazonSocial = "Maria Lopez SA",
                RFC = "MALO850202ABC",
                RegimenFiscal = "601",
                RegimenFiscalDescripcion = "General de Ley",
                UsoCFDI = "G01",
                UsoCFDIDescripcion = "Adquisición de mercancías",
                Estado = "Completada",
                FechaSolicitud = DateTime.UtcNow.AddDays(-1)
            }
        };
    }

    [Fact]
    public void Facturas_WhenAuthorized_ShouldLoadAndDisplaySolicitudes()
    {
        // Arrange
        var authState = CreateAuthState("admin");
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllSolicitudesFacturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSolicitudes());

        // Act
        var cut = Context.Render<Facturas>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Gestión de Solicitudes de Factura"));
        cut.Markup.Should().Contain("#101");
        cut.Markup.Should().Contain("Juan Perez");
        cut.Markup.Should().Contain("#102");
        cut.Markup.Should().Contain("Maria Lopez");
        cut.Markup.Should().Contain("Total de solicitudes: 2");
    }

    [Fact]
    public void Facturas_WhenNotAuthorized_ShouldDisplayNotAuthorizedMessage()
    {
        // Arrange
        var authState = CreateAuthState("guest");
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);

        // Act
        var cut = Context.Render<Facturas>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Assert
        cut.Markup.Should().Contain("No tienes permisos para ver esta página.");
        cut.Markup.Should().NotContain("Gestión de Solicitudes de Factura");
    }

    [Fact]
    public async Task Facturas_WhenFilterBySearchString_ShouldFilterList()
    {
        // Arrange
        var authState = CreateAuthState("supervisor");
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllSolicitudesFacturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSolicitudes());

        var cut = Context.Render<Facturas>(p => p.AddCascadingValue(Task.FromResult(authState)));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Juan Perez"));

        // Act - Search for Maria
        var searchField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => searchField.Instance.ValueChanged.InvokeAsync("Maria"));

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Maria Lopez");
            cut.Markup.Should().NotContain("Juan Perez");
            cut.Markup.Should().Contain("Total de solicitudes: 1");
        });
    }

    [Fact]
    public async Task Facturas_WhenFilterByEstado_ShouldFilterList()
    {
        // Arrange
        var authState = CreateAuthState("admin");
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllSolicitudesFacturaQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateSampleSolicitudes());

        var cut = Context.Render<Facturas>(p => p.AddCascadingValue(Task.FromResult(authState)));
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Juan Perez"));

        // Act - Change estado filter to Completada
        var select = cut.FindComponent<MudSelect<string>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync("Completada"));

        // Assert
        cut.Markup.Should().Contain("Maria Lopez");
        cut.Markup.Should().NotContain("Juan Perez");
        cut.Markup.Should().Contain("Total de solicitudes: 1");
    }

    [Fact]
    public void Facturas_WhenLoadDataThrows_ShouldShowSnackbarError()
    {
        // Arrange
        var authState = CreateAuthState("admin");
        _mockAuthStateProvider.Setup(a => a.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllSolicitudesFacturaQuery>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database timeout"));

        // Act
        var cut = Context.Render<Facturas>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Assert
        _mockSnackbar.Verify(s => s.Add("Error al cargar solicitudes: Database timeout", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }
}
