using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Dialogs;
using BotCarniceria.Core.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MudBlazor;
using MudBlazor.Services;
using MediatR;
using FluentAssertions;

namespace BotCarniceria.Presentation.Blazor.Tests.Dialogs;

public class PedidoDetailDialogTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private IRenderedComponent<MudDialogProvider> _dialogProvider = default!;

    public PedidoDetailDialogTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        Context.Services.AddSingleton(_mockMediator.Object);
        // Real IDialogService used
        
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        Context.Render<MudSnackbarProvider>();
        _dialogProvider = Context.Render<MudDialogProvider>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public async Task Dialog_ShouldRenderPedidoDetails()
    {
        // Arrange
        var pedido = new PedidoDto 
        { 
            PedidoID = 1,
            Folio = "FOL-12345",
            ClienteNombre = "Juan",
            ClienteTelefono = "123",
            Estado = "EnEspera",
            Fecha = DateTime.Now,
            Contenido = "Carne 1kg\nPollo 2kg",
            Notas = "Test Note"
        };
        
        // Act
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["Pedido"] = pedido };
        
        await _dialogProvider.InvokeAsync(() => dialogService.Show<PedidoDetailDialog>("Title", parameters));

        // Assert
        _dialogProvider.WaitForAssertion(() => _dialogProvider.Markup.Should().Contain("FOL-12345"));
        _dialogProvider.Markup.Should().Contain("Carne");
    }
}
