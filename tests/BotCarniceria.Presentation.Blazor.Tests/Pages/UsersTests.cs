using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using BotCarniceria.Presentation.Blazor.Components.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.CQRS.Commands;
using MediatR;
using MudBlazor;
using MudBlazor.Services;
using FluentAssertions;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class UsersTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ISnackbar> _mockSnackbar;
    private readonly Mock<IDialogService> _mockDialogService;

    public UsersTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockSnackbar = new Mock<ISnackbar>();
        _mockDialogService = new Mock<IDialogService>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddSingleton(_mockMediator.Object);
        Context.Services.AddSingleton(_mockSnackbar.Object);
        Context.Services.AddSingleton(_mockDialogService.Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void UsersPage_ShouldLoadAndShowUsers_WhenMediatorReturnsData()
    {
        // Arrange
        var usersList = new List<UsuarioDto>
        {
            new UsuarioDto { UsuarioID = 1, NombreUsuario = "user1", NombreCompleto = "User One", Rol = "Admin", Activo = true },
            new UsuarioDto { UsuarioID = 2, NombreUsuario = "user2", NombreCompleto = "User Two", Rol = "Editor", Activo = false }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

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
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsuarioDto>());

        // Act
        var cut = Context.Render<Users>();

        // Assert
        var btn = cut.Find("button");
        btn.TextContent.Should().Contain("Nuevo Usuario");
    }

    [Fact]
    public void UsersPage_WhenUserIsLocked_ShouldShowLockedChipAndUnlockButton()
    {
        // Arrange
        var usersList = new List<UsuarioDto>
        {
            new UsuarioDto { UsuarioID = 5, NombreUsuario = "lockedUser", NombreCompleto = "Locked", Rol = "Admin", Activo = true, IsLocked = true }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        // Act
        var cut = Context.Render<Users>();

        // Assert
        cut.Markup.Should().Contain("Bloqueado");
        var unlockBtn = cut.Find("button[aria-label='Desbloquear usuario']");
        unlockBtn.Should().NotBeNull();
    }

    [Fact]
    public async Task UsersPage_UnlockUser_Success_ShouldCallMediatorAndShowSnackbar()
    {
        // Arrange
        var usersList = new List<UsuarioDto>
        {
            new UsuarioDto { UsuarioID = 5, NombreUsuario = "lockedUser", NombreCompleto = "Locked", Rol = "Admin", Activo = true, IsLocked = true }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);
        _mockMediator.Setup(m => m.Send(It.IsAny<ResetUserLockoutCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Context.Render<Users>();

        // Act
        var unlockBtn = cut.Find("button[aria-label='Desbloquear usuario']");
        await cut.InvokeAsync(() => unlockBtn.Click());

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<ResetUserLockoutCommand>(c => c.UsuarioID == 5), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("Usuario lockedUser desbloqueado", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UsersPage_ToggleStatus_ShouldCallMediatorAndShowSnackbar()
    {
        // Arrange
        var usersList = new List<UsuarioDto>
        {
            new UsuarioDto { UsuarioID = 3, NombreUsuario = "activeUser", NombreCompleto = "Active", Rol = "Editor", Activo = true }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);
        _mockMediator.Setup(m => m.Send(It.IsAny<ToggleUsuarioActivoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Context.Render<Users>();

        // Act - Click toggle status button (currently active, so label is "Desactivar usuario")
        var toggleBtn = cut.Find("button[aria-label='Desactivar usuario']");
        await cut.InvokeAsync(() => toggleBtn.Click());

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<ToggleUsuarioActivoCommand>(c => c.UsuarioID == 3 && c.Activo == false), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("Usuario desactivado", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UsersPage_ChangePassword_ShouldShowInfoSnackbar()
    {
        // Arrange
        var usersList = new List<UsuarioDto>
        {
            new UsuarioDto { UsuarioID = 4, NombreUsuario = "pwdUser", NombreCompleto = "Pwd", Rol = "Editor", Activo = true }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(usersList);

        var cut = Context.Render<Users>();

        // Act
        var resetPwdBtn = cut.Find("button[aria-label='Restablecer contraseña']");
        await cut.InvokeAsync(() => resetPwdBtn.Click());

        // Assert
        _mockSnackbar.Verify(s => s.Add("Funcionalidad pendiente: Cambiar contraseña", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task UsersPage_AddUser_ShouldOpenUserDialog()
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsuarioDto>());

        var mockDialogRef = new Mock<IDialogReference>();
        mockDialogRef.Setup(d => d.Result).ReturnsAsync(DialogResult.Ok(true));
        _mockDialogService.SetReturnsDefault<Task<IDialogReference>>(Task.FromResult(mockDialogRef.Object));

        var cut = Context.Render<Users>();

        // Act
        var addBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Nuevo Usuario"));
        await cut.InvokeAsync(() => addBtn.Click());

        // Assert
        _mockDialogService.Verify(d => d.ShowAsync<UserDialog>("Nuevo Usuario"), Times.Once);
    }

    [Fact]
    public async Task UsersPage_EditUser_ShouldOpenUserDialogWithParameters()
    {
        // Arrange
        var user = new UsuarioDto { UsuarioID = 10, NombreUsuario = "editMe", NombreCompleto = "Edit", Rol = "Admin", Activo = true };
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllUsuariosQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UsuarioDto> { user });

        var mockDialogRef = new Mock<IDialogReference>();
        mockDialogRef.Setup(d => d.Result).ReturnsAsync(DialogResult.Ok(true));
        _mockDialogService.SetReturnsDefault<Task<IDialogReference>>(Task.FromResult(mockDialogRef.Object));

        var cut = Context.Render<Users>();

        // Act
        var editBtn = cut.Find("button[aria-label='Editar usuario']");
        await cut.InvokeAsync(() => editBtn.Click());

        // Assert
        _mockDialogService.Verify(d => d.ShowAsync<UserDialog>("Editar Usuario", It.IsAny<DialogParameters<UserDialog>>()), Times.Once);
    }
}
