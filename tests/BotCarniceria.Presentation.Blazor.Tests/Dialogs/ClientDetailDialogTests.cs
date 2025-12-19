using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Dialogs;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MudBlazor;
using MudBlazor.Services;
using MediatR;
using FluentAssertions;

namespace BotCarniceria.Presentation.Blazor.Tests.Dialogs;

public class ClientDetailDialogTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private IRenderedComponent<MudDialogProvider> _dialogProvider = default!;

    public ClientDetailDialogTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        Context.Services.AddSingleton(_mockMediator.Object);
        // Note: We use real IDialogService from AddMudServices
        
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        Context.Render<MudSnackbarProvider>();
        
        // Render and capture the DialogProvider
        _dialogProvider = Context.Render<MudDialogProvider>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public async Task Dialog_ShouldLoadClientDetails()
    {
        // Arrange
        var cliente = new ClienteDto 
        { 
            ClienteID = 1, 
            Nombre = "Test Client", 
            NumeroTelefono = "5555555555",
            Activo = true
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetClienteByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cliente);
            
        _mockMediator.Setup(m => m.Send(It.IsAny<GetPedidosByClienteQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PedidoDto>());

        // Act
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["ClienteId"] = 1 };
        
        await _dialogProvider.InvokeAsync(() => dialogService.Show<ClientDetailDialog>("Test Title", parameters));

        // Assert
        _dialogProvider.WaitForAssertion(() => _dialogProvider.Markup.Should().Contain("Test Client"));
        _dialogProvider.Markup.Should().Contain("5555555555");
    }
}
