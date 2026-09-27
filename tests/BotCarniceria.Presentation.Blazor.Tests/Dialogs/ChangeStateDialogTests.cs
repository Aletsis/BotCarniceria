using Bunit;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Presentation.Blazor.Components.Dialogs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Dialogs;

public class ChangeStateDialogTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private IRenderedComponent<MudDialogProvider> _dialogProvider = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
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
    public async Task ChangeStateDialog_ShouldRenderWithInitialValues()
    {
        // Arrange
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["CurrentState"] = ConversationState.TAKING_ORDER };

        // Act
        await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ChangeStateDialog>("Test Title", parameters));

        // Assert
        _dialogProvider.WaitForAssertion(() => _dialogProvider.Markup.Should().Contain("Seleccione el nuevo estado para la conversación."));
        _dialogProvider.Markup.Should().Contain("TAKING_ORDER");
    }

    [Fact]
    public async Task ChangeStateDialog_Cancel_ShouldCancelDialog()
    {
        // Arrange
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["CurrentState"] = ConversationState.START };
        var dialogRef = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ChangeStateDialog>("Test Title", parameters));

        // Act
        var cancelButton = _dialogProvider.FindAll("button").First(b => b.TextContent.Contains("Cancelar"));
        await _dialogProvider.InvokeAsync(() => cancelButton.Click());

        // Assert
        var result = await dialogRef.Result;
        result.Should().NotBeNull();
        result!.Canceled.Should().BeTrue();
    }

    [Fact]
    public async Task ChangeStateDialog_Submit_ShouldCloseWithSelectedState()
    {
        // Arrange
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["CurrentState"] = ConversationState.ASK_NAME };
        var dialogRef = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ChangeStateDialog>("Test Title", parameters));

        // Act
        var saveButton = _dialogProvider.FindAll("button").First(b => b.TextContent.Contains("Guardar"));
        await _dialogProvider.InvokeAsync(() => saveButton.Click());

        // Assert
        var result = await dialogRef.Result;
        result.Should().NotBeNull();
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(ConversationState.ASK_NAME);
    }
}
