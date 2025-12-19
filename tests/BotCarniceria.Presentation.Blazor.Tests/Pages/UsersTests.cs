using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.CQRS.Queries;
using MediatR;
using MudBlazor;
using MudBlazor.Services;
using FluentAssertions;
using System.Threading.Tasks;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class UsersTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        
        // Register MudBlazor services
        Context.Services.AddMudServices();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
        }
    }

    [Fact]
    public void UsersPage_ShouldLoadAndShowUsers_WhenMediatorReturnsData()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        var usersList = new List<UsuarioDto>
        {
            new UsuarioDto { UsuarioID = 1, NombreUsuario = "user1", NombreCompleto = "User One", Rol = "Admin", Activo = true },
            new UsuarioDto { UsuarioID = 2, NombreUsuario = "user2", NombreCompleto = "User Two", Rol = "Editor", Activo = false }
        };

        mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        Context.Services.AddSingleton(mockMediator.Object);

        // Render Providers
        Context.Render<MudPopoverProvider>();
        Context.Render<MudDialogProvider>();
        Context.Render<MudSnackbarProvider>();

        // Act
        var cut = Context.Render<Users>();

        // Assert
        cut.Find("h5").TextContent.Should().Contain("Gestión de Usuarios");
        
        var rows = cut.FindAll("tbody tr");
        rows.Count.Should().Be(2);

        rows[0].TextContent.Should().Contain("user1");
        rows[1].TextContent.Should().Contain("user2");
    }

    [Fact]
    public void UsersPage_ShouldShowAddButton()
    {
        // Arrange
        var mockMediator = new Mock<IMediator>();
        mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsuarioDto>());
        
        Context.Services.AddSingleton(mockMediator.Object);

        // Render Providers
        Context.Render<MudPopoverProvider>();
        Context.Render<MudDialogProvider>();
        Context.Render<MudSnackbarProvider>();

        // Act
        var cut = Context.Render<Users>();

        // Assert
        var btn = cut.Find("button");
        btn.TextContent.Should().Contain("Nuevo Usuario");
    }
}
