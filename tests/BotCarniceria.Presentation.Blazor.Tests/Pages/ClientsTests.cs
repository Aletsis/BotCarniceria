using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MudBlazor;
using MudBlazor.Services;
using MediatR;
using FluentAssertions;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using BotCarniceria.Presentation.Blazor.Components.Dialogs;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class ClientsTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;

    public ClientsTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();

        // Auth
        Context.Services.AddAuthorizationCore();
        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Context.Services.AddSingleton(mockAuthService.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "admin") };
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test")));
        Context.Services.AddSingleton(new Mock<AuthenticationStateProvider>().Object); // Just placeholder, using cascading

        Context.Services.AddSingleton(_mockMediator.Object);
        Context.Services.AddSingleton(new Mock<IDialogService>().Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        Context.Render<MudDialogProvider>();
        Context.Render<MudSnackbarProvider>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void ClientsPage_ShouldLoadAndShowClients()
    {
        // Arrange
        var clients = new List<ClienteDto> 
        { 
            new ClienteDto { ClienteID = 1, Nombre = "Juan Perez", NumeroTelefono = "5551234567", Activo = true } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllClientesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);

        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test")));

        // Act
        var cut = Context.Render<Clients>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);
        cut.Markup.Should().Contain("Juan Perez");
        cut.Markup.Should().Contain("5551234567");
    }

    [Fact]
    public void ClientsPage_ShouldFilterClients()
    {
        // Arrange
        var clients = new List<ClienteDto> 
        { 
            new ClienteDto { ClienteID = 1, Nombre = "Juan Perez", NumeroTelefono = "5551234567", Activo = true },
            new ClienteDto { ClienteID = 2, Nombre = "Maria Lopez", NumeroTelefono = "5559876543", Activo = true }
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllClientesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);

        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test")));

        var cut = Context.Render<Clients>(p => p.AddCascadingValue(Task.FromResult(authState)));
        cut.WaitForState(() => cut.FindAll("tr").Count >= 3); // Header + 2 rows

        // Act
        // Find the search text field and input value
        var searchInput = cut.FindAll("input").FirstOrDefault(i => i.GetAttribute("placeholder")?.Contains("Buscar") == true);
        searchInput?.Input("Maria");
        
        // Wait for debounce or manually trigger if possible. 
        // Since DebounceInterval is 300ms, we might need to wait or trigger the method directly if exposed, 
        // but here we simulate the user interaction. 
        // However, MudTextField debounce might be tricky in tests without waiting.
        // Assuming we can trigger the 'ApplyFilters' button or wait.
        // There is an "Aplicar Filtros" button.
        
        var filterButton = cut.FindComponents<MudButton>()
            .FirstOrDefault(b => b.Markup.Contains("Aplicar Filtros"));
        filterButton?.Find("button").Click();

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count == 2); // Header + 1 row
        cut.Markup.Should().Contain("Maria Lopez");
        cut.Markup.Should().NotContain("Juan Perez");
    }

    [Fact]
    public void ClientsPage_ShouldOpenCreateDialog()
    {
        // Arrange
        var clients = new List<ClienteDto>();
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllClientesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);
        
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test")));
        var cut = Context.Render<Clients>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Mock Dialog Service to verify call
        var dialogServiceMock = Mock.Get(Context.Services.GetService<IDialogService>()!);
        // Setup ShowAsync to return a dummy result to avoid null reference if awaited
        var dialogReference = new Mock<IDialogReference>();
        dialogReference.Setup(x => x.Result).ReturnsAsync(DialogResult.Ok(true));
        dialogServiceMock.Setup(d => d.ShowAsync<EditClientDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReference.Object);

        // Act
        var newButton = cut.FindComponents<MudButton>()
            .FirstOrDefault(b => b.Markup.Contains("Nuevo"));
        newButton?.Find("button").Click();

        // Assert
        dialogServiceMock.Verify(d => d.ShowAsync<EditClientDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()), Times.Once);
    }

    [Fact]
    public void ClientsPage_ShouldToggleStatus()
    {
        // Arrange
        var client = new ClienteDto { ClienteID = 1, Nombre = "Juan Perez", Activo = true };
        var clients = new List<ClienteDto> { client };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllClientesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clients);
        
        // Setup Toggle Command
        _mockMediator.Setup(m => m.Send(It.IsAny<ToggleClienteActivoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test")));
        var cut = Context.Render<Clients>(p => p.AddCascadingValue(Task.FromResult(authState)));
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);

        // Mock Dialog Service for Confirmation
        var dialogServiceMock = Mock.Get(Context.Services.GetService<IDialogService>()!);
        var dialogReference = new Mock<IDialogReference>();
        dialogReference.Setup(x => x.Result).ReturnsAsync(DialogResult.Ok(true)); // User clicks "Yes"
        dialogServiceMock.Setup(d => d.ShowAsync<BotCarniceria.Presentation.Blazor.Components.Shared.ConfirmDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReference.Object);

        // Act
        // Find the toggle button (it's an IconButton with specific icon or tooltip)
        // We can look for the tooltip text "Desactivar" or the icon
        var toggleButton = cut.FindAll("button").FirstOrDefault(b => b.OuterHtml.Contains("Desactivar"));
        toggleButton?.Click();

        // Assert
        // Verify dialog was shown
        dialogServiceMock.Verify(d => d.ShowAsync<BotCarniceria.Presentation.Blazor.Components.Shared.ConfirmDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()), Times.Once);
        
        // Verify Mediator command was sent
        _mockMediator.Verify(m => m.Send(It.Is<ToggleClienteActivoCommand>(c => c.ClienteID == 1 && c.Activo == false), It.IsAny<CancellationToken>()), Times.Once);
    }
}
