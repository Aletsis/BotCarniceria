using Bunit;
using Bunit.TestDoubles;
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

public class OrdersTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private Mock<AuthenticationStateProvider> _mockAuthStateProvider;

    public OrdersTests()
    {
         _mockMediator = new Mock<IMediator>(); 
         _mockAuthStateProvider = new Mock<AuthenticationStateProvider>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        // Manual Auth Setup
        Context.Services.AddAuthorizationCore();

        // Mocking AuthorizationService to bypass bUnit placeholder and guarantee success
        var mockAuthService = new Mock<IAuthorizationService>();
        mockAuthService.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object?>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
            .ReturnsAsync(AuthorizationResult.Success());
        Context.Services.AddSingleton(mockAuthService.Object);

        var claims = new List<Claim> 
        { 
            new Claim(ClaimTypes.Name, "Admin User"), 
            new Claim(ClaimTypes.Role, "admin") 
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        
        // No need to register provider if we pass cascading value, but let's keep it consistent
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);
        Context.Services.AddSingleton<AuthenticationStateProvider>(_mockAuthStateProvider.Object);

        // Mediator registration from field
        Context.Services.AddSingleton(_mockMediator.Object);
        
        // Mock IDialogService (needed for details)
        Context.Services.AddSingleton(new Mock<IDialogService>().Object);
        
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // Render MudBlazor Providers
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
    public void OrdersPage_ShouldLoadAndShowOrders()
    {
        // ... (existing test content)
                // Arrange
        var orders = new List<PedidoDto>
        {
            new PedidoDto 
            { 
                PedidoID = 1, 
                Folio = "P-001", 
                ClienteNombre = "Cliente 1", 
                Estado = "EnEspera", 
                Fecha = DateTime.Now,
                ClienteTelefono = "123"
            },
            new PedidoDto 
            { 
                PedidoID = 2, 
                Folio = "P-002", 
                ClienteNombre = "Cliente 2", 
                Estado = "Entregado", 
                Fecha = DateTime.Now,
                ClienteTelefono = "456"
            }
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Auth Setup
        var claims = new List<Claim> 
        { 
            new Claim(ClaimTypes.Name, "Admin User"), 
            new Claim(ClaimTypes.Role, "admin") 
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);

        // Act
        var cut = Context.Render<Orders>(parameters => 
            parameters.AddCascadingValue(Task.FromResult(authState))
        );

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count >= 3); 

        cut.Markup.Should().Contain("P-001");
        cut.Markup.Should().Contain("Cliente 1");
        cut.Markup.Should().Contain("P-002");
        cut.Markup.Should().Contain("Cliente 2");
    }

    [Fact]
    public void OrdersPage_ShouldFilterByDefaultDateForEditor()
    {
        // Arrange
        var today = BotCarniceria.Shared.Helpers.TimeZoneHelper.Now;
        var yesterday = today.AddDays(-1);

        var orders = new List<PedidoDto>
        {
            new PedidoDto { PedidoID = 1, Folio = "P-Old", Fecha = BotCarniceria.Shared.Helpers.TimeZoneHelper.ToUtcTime(yesterday), Estado = "EnEspera" },
            new PedidoDto { PedidoID = 2, Folio = "P-New", Fecha = BotCarniceria.Shared.Helpers.TimeZoneHelper.ToUtcTime(today), Estado = "EnEspera" }
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Editor Role
        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "editor") };
        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth")));
        
        // Update Mock for Injection
        _mockAuthStateProvider.Setup(x => x.GetAuthenticationStateAsync()).ReturnsAsync(authState);

        // Act
        var cut = Context.Render<Orders>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("P-New"));
        cut.Markup.Should().NotContain("P-Old");
    }

    [Fact]
    public void OrdersPage_ShouldViewDetails()
    {
        // Arrange
        var order = new PedidoDto { PedidoID = 1, Folio = "P-001", ClienteNombre = "Cliente 1", Fecha = DateTime.UtcNow, Estado = "EnEspera" };
        var orders = new List<PedidoDto> { order };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "TestAuth")));

        var cut = Context.Render<Orders>(p => p.AddCascadingValue(Task.FromResult(authState)));

        // Mock Dialog Service
        var dialogServiceMock = Mock.Get(Context.Services.GetService<IDialogService>()!);
        var dialogReference = new Mock<IDialogReference>();
        dialogReference.Setup(x => x.Result).ReturnsAsync(DialogResult.Ok(true));
        dialogServiceMock.Setup(d => d.ShowAsync<PedidoDetailDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReference.Object);

        // Act
        var detailsButton = cut.WaitForElement("button[aria-label='Ver detalles del pedido']");
        detailsButton.Click();

        // Assert
        dialogServiceMock.Verify(d => d.ShowAsync<PedidoDetailDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()), Times.Once);
    }
    
    [Fact]
    public async Task OrdersPage_ShouldChangeStatus()
    {
        // Arrange
        var order = new PedidoDto { PedidoID = 1, Folio = "P-001", ClienteNombre = "C1", Fecha = DateTime.Now, Estado = "EnEspera" };
        var orders = new List<PedidoDto> { order };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllPedidosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);
            
        _mockMediator.Setup(m => m.Send(It.IsAny<UpdatePedidoEstadoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var authState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "TestAuth")));

        var cut = Context.Render<Orders>(p => p.AddCascadingValue(Task.FromResult(authState)));
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);

        // Mock MessageBox
        var dialogServiceMock = Mock.Get(Context.Services.GetService<IDialogService>()!);
        dialogServiceMock.Setup(d => d.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(true); // User says Yes

        // Act
        // Trigger status change. This usually happens via a dropdown or buttons in OrderTable.
        // Assuming OrderTable fires OnStatusChanged.
        // I can trigger the EventCallback of the OrderTable component directly to simulate the child component's action.
        var orderTable = cut.FindComponent<BotCarniceria.Presentation.Blazor.Components.Orders.OrderTable>();
        
        await cut.InvokeAsync(() => orderTable.Instance.OnStatusChanged.InvokeAsync(new Tuple<PedidoDto, string>(order, "EnRuta")));

        // Assert
        dialogServiceMock.Verify(d => d.ShowMessageBox(It.IsAny<string>(), It.IsAny<string>(), "Sí", null, "Cancelar", It.IsAny<DialogOptions>()), Times.Once);
        _mockMediator.Verify(m => m.Send(It.Is<UpdatePedidoEstadoCommand>(c => c.PedidoID == 1 && c.NuevoEstado == "EnRuta"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
