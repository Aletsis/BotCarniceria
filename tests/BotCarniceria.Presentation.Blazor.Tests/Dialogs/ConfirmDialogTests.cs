using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Shared;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Dialogs;

public class ConfirmDialogTests : IAsyncLifetime
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
    public async Task ConfirmDialog_ShouldRenderContentAndButtons()
    {
        // Arrange
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            ["ContentText"] = "¿Deseas eliminar este registro?",
            ["SubmitText"] = "Eliminar",
            ["Color"] = Color.Error
        };

        // Act
        await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ConfirmDialog>("Confirmación", parameters));

        // Assert
        _dialogProvider.WaitForAssertion(() =>
        {
            _dialogProvider.Markup.Should().Contain("¿Deseas eliminar este registro?");
            _dialogProvider.Markup.Should().Contain("Eliminar");
            _dialogProvider.Markup.Should().Contain("Cancelar");
        });
    }

    [Fact]
    public async Task ConfirmDialog_Submit_ShouldCloseWithOkResult()
    {
        // Arrange
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            ["ContentText"] = "¿Confirmar acción?",
            ["SubmitText"] = "Aceptar"
        };
        var dialogRef = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ConfirmDialog>("Confirmar", parameters));

        // Act
        var submitButton = _dialogProvider.FindAll("button").First(b => b.TextContent.Contains("Aceptar"));
        await _dialogProvider.InvokeAsync(() => submitButton.Click());

        // Assert
        var result = await dialogRef.Result;
        result.Should().NotBeNull();
        result!.Canceled.Should().BeFalse();
        result.Data.Should().Be(true);
    }

    [Fact]
    public async Task ConfirmDialog_Cancel_ShouldCancelDialog()
    {
        // Arrange
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters
        {
            ["ContentText"] = "¿Confirmar acción?"
        };
        var dialogRef = await _dialogProvider.InvokeAsync(() => dialogService.ShowAsync<ConfirmDialog>("Confirmar", parameters));

        // Act
        var cancelButton = _dialogProvider.FindAll("button").First(b => b.TextContent.Contains("Cancelar"));
        await _dialogProvider.InvokeAsync(() => cancelButton.Click());

        // Assert
        var result = await dialogRef.Result;
        result.Should().NotBeNull();
        result!.Canceled.Should().BeTrue();
    }
}
