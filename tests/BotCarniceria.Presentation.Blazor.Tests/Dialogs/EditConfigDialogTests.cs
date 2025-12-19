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

public class EditConfigDialogTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private IRenderedComponent<MudDialogProvider> _dialogProvider = default!;

    public EditConfigDialogTests()
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
    public async Task Dialog_ShouldRenderConfigValues()
    {
        // Arrange
        var config = new ConfiguracionDto 
        { 
            Clave = "TestKey", 
            Valor = "TestVal",
            Editable = true
        };
        
        // Act
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["Config"] = config };
        
        await _dialogProvider.InvokeAsync(() => dialogService.Show<EditConfigDialog>("Title", parameters));

        // Assert
        _dialogProvider.WaitForAssertion(() => _dialogProvider.Markup.Should().Contain("TestKey"));
        _dialogProvider.Markup.Should().Contain("TestVal");
    }
}
