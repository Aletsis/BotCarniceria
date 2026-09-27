using Bunit;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Presentation.Blazor.Components.Dialogs;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class ConversationsTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IDialogService> _mockDialogService;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private IRenderedComponent<MudPopoverProvider> _popoverProvider = default!;

    public ConversationsTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockDialogService = new Mock<IDialogService>();
        _mockSnackbar = new Mock<ISnackbar>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        Context.Services.AddSingleton(_mockMediator.Object);
        Context.Services.AddSingleton(_mockDialogService.Object);
        Context.Services.AddSingleton(_mockSnackbar.Object);

        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        _popoverProvider = Context.Render<MudPopoverProvider>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void ConversationsPage_ShouldLoadList()
    {
        // Arrange
        var list = new List<ConversacionDto> 
        { 
            new ConversacionDto { NumeroTelefono = "123", Estado = "START", NombreTemporal = "Temp", UltimaActividad = DateTime.Now } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Act
        var cut = Context.Render<Conversations>();

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);
        cut.Markup.Should().Contain("123");
        cut.Markup.Should().Contain("Temp");
    }

    [Fact]
    public void ConversationsPage_WhenConversationExpired_ShouldShowExpiradaChip()
    {
        // Arrange
        var list = new List<ConversacionDto> 
        { 
            new ConversacionDto { NumeroTelefono = "999", Estado = "MENU", EstaExpirada = true, UltimaActividad = DateTime.Now } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Act
        var cut = Context.Render<Conversations>();

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("EXPIRADA"));
    }

    [Fact]
    public async Task ConversationsPage_ClearBuffer_WhenConfirmed_ShouldUpdateSessionAndShowSnackbar()
    {
        // Arrange
        var list = new List<ConversacionDto> 
        { 
            new ConversacionDto { NumeroTelefono = "5551112233", Estado = "TAKING_ORDER", UltimaActividad = DateTime.Now } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateSessionStateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockDialogService.Setup(d => d.ShowMessageBox(
            "Confirmación",
            It.Is<string>(s => s.Contains("¿Limpiar buffer de 5551112233?")),
            "Sí",
            null,
            "Cancelar",
            It.IsAny<DialogOptions>()))
            .ReturnsAsync(true);

        var cut = Context.Render<Conversations>();
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);

        var btn = cut.Find("button[aria-label='Opciones de conversación']");
        btn.Click();

        var clearItem = _popoverProvider.FindComponents<MudMenuItem>().First(m => m.Instance.Icon == Icons.Material.Filled.CleaningServices);
        await cut.InvokeAsync(() => clearItem.Instance.OnClick.InvokeAsync());

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<UpdateSessionStateCommand>(c => c.PhoneNumber == "5551112233" && c.Buffer == ""), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("Buffer limpiado", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConversationsPage_ResetSession_WhenConfirmed_ShouldResetSessionAndShowSnackbar()
    {
        // Arrange
        var list = new List<ConversacionDto> 
        { 
            new ConversacionDto { NumeroTelefono = "5554445566", Estado = "START", UltimaActividad = DateTime.Now } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _mockMediator.Setup(m => m.Send(It.IsAny<ResetSessionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockDialogService.Setup(d => d.ShowMessageBox(
            "Confirmación",
            It.Is<string>(s => s.Contains("¿Está seguro que desea resetear la sesión de 5554445566?")),
            "Sí",
            null,
            "Cancelar",
            It.IsAny<DialogOptions>()))
            .ReturnsAsync(true);

        var cut = Context.Render<Conversations>();
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);

        var btn = cut.Find("button[aria-label='Opciones de conversación']");
        btn.Click();

        // Act - Invoke ResetSession menu item
        var resetItem = _popoverProvider.FindComponents<MudMenuItem>().First(m => m.Instance.Icon == Icons.Material.Filled.Refresh);
        await cut.InvokeAsync(() => resetItem.Instance.OnClick.InvokeAsync());

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<ResetSessionCommand>(c => c.PhoneNumber == "5554445566"), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("Sesión reseteada correctamente", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ConversationsPage_ChangeState_ShouldOpenChangeStateDialogAndUpdate()
    {
        // Arrange
        var list = new List<ConversacionDto> 
        { 
            new ConversacionDto { NumeroTelefono = "5557778899", Estado = "START", UltimaActividad = DateTime.Now } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);
        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateSessionStateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var mockDialogRef = new Mock<IDialogReference>();
        mockDialogRef.Setup(d => d.Result).ReturnsAsync(DialogResult.Ok(ConversationState.MENU));

        _mockDialogService.Setup(d => d.ShowAsync<ChangeStateDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>()))
            .ReturnsAsync(mockDialogRef.Object);
        _mockDialogService.Setup(d => d.ShowAsync<ChangeStateDialog>(It.IsAny<string>(), It.IsAny<DialogParameters>(), It.IsAny<DialogOptions>()))
            .ReturnsAsync(mockDialogRef.Object);

        var cut = Context.Render<Conversations>();
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);

        var btn = cut.Find("button[aria-label='Opciones de conversación']");
        btn.Click();

        // Act - Invoke ChangeState menu item
        var changeStateItem = _popoverProvider.FindComponents<MudMenuItem>().First(m => m.Instance.Icon == Icons.Material.Filled.Edit);
        await cut.InvokeAsync(() => changeStateItem.Instance.OnClick.InvokeAsync());

        // Assert
        _mockDialogService.Verify(d => d.ShowAsync<ChangeStateDialog>("Cambiar Estado Manualmente", It.IsAny<DialogParameters>()), Times.Once);
        _mockMediator.Verify(m => m.Send(It.Is<UpdateSessionStateCommand>(c => c.PhoneNumber == "5557778899" && c.NewState == "MENU"), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("Estado cambiado a MENU", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }
}
