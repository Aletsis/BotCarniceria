using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Controllers;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Moq;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Presentation.Blazor.Tests.Controllers;

public class AccountControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IAuthenticationService> _mockAuthService;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IUrlHelper> _mockUrlHelper;
    private readonly AccountController _controller;

    public AccountControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockAuthService = new Mock<IAuthenticationService>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockUrlHelper = new Mock<IUrlHelper>();

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IAuthenticationService)))
            .Returns(_mockAuthService.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IUrlHelperFactory)))
            .Returns(new Mock<IUrlHelperFactory>().Object);

        var httpContext = new DefaultHttpContext
        {
            RequestServices = _mockServiceProvider.Object
        };

        _controller = new AccountController(_mockMediator.Object, new Mock<ILogger<AccountController>>().Object)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            Url = _mockUrlHelper.Object
        };
    }

    [Fact]
    public async Task Login_ValidCredentials_ShouldSignInAndRedirectHome()
    {
        // Arrange
        var username = "admin";
        var password = "password";
        var userDto = new UsuarioDto { UsuarioID = 1, NombreUsuario = username, NombreCompleto = "Admin User", Rol = "Admin" };

        _mockMediator.Setup(m => m.Send(It.Is<LoginUserQuery>(q => q.Username == username && q.Password == password), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);
        
        _mockAuthService.Setup(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Login(username, password, null!);

        // Assert
        var redirect = result.Should().BeOfType<LocalRedirectResult>().Subject;
        redirect.Url.Should().Be("/");
        
        // Verify SignIn called
        _mockAuthService.Verify(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ShouldRedirectToLoginWithError()
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<LoginUserQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UsuarioDto?)null);

        // Act
        var result = await _controller.Login("bad", "guy", null!);

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Contain("/login?error=");
        
        _mockAuthService.Verify(s => s.SignInAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<System.Security.Claims.ClaimsPrincipal>(), It.IsAny<AuthenticationProperties>()), Times.Never);
    }

    [Fact]
    public async Task Logout_ShouldSignOutAndRedirect()
    {
        // Act
        var result = await _controller.Logout();

        // Assert
        var redirect = result.Should().BeOfType<RedirectResult>().Subject;
        redirect.Url.Should().Be("/login");

        _mockAuthService.Verify(s => s.SignOutAsync(It.IsAny<HttpContext>(), It.IsAny<string>(), It.IsAny<AuthenticationProperties>()), Times.Once);
    }
}
