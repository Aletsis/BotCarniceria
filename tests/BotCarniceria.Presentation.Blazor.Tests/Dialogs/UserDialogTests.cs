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

public class UserDialogTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private IRenderedComponent<MudDialogProvider> _dialogProvider = default!;

    public UserDialogTests()
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
    public async Task Dialog_ShouldRenderUserForm()
    {
        // Arrange
        var usuario = new UsuarioDto 
        { 
            UsuarioID = 1,
            NombreUsuario = "admin1",
            NombreCompleto = "Administrator",
            Rol = "Admin"
        };
        
        // Act
        var dialogService = Context.Services.GetRequiredService<IDialogService>();
        var parameters = new DialogParameters { ["User"] = usuario };
        
        await _dialogProvider.InvokeAsync(() => dialogService.Show<UserDialog>("Title", parameters));

        // Assert
        _dialogProvider.WaitForAssertion(() => _dialogProvider.Markup.Should().Contain("admin1"));
        _dialogProvider.Markup.Should().Contain("Administrator");
    }
}
