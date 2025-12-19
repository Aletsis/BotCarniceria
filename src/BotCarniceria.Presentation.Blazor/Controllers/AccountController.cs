using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using MediatR;
using BotCarniceria.Core.Application.CQRS.Queries;

namespace BotCarniceria.Presentation.Blazor.Controllers;

[Route("account")]
public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly IMediator _mediator;

    public AccountController(IMediator mediator, ILogger<AccountController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password, [FromForm] string returnUrl)
    {
        try
        {
            _logger.LogInformation("Attempting login for user: {Username}", username);
            var user = await _mediator.Send(new LoginUserQuery(username, password));

            if (user != null)
            {
                _logger.LogInformation("Login successful for user: {Username}", username);
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.NombreUsuario),
                    new Claim(ClaimTypes.Role, user.Rol.ToLower()),
                    new Claim("UserId", user.UsuarioID.ToString()),
                    new Claim("FullName", user.NombreCompleto)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                
                // Use default expiration from Program.cs (sliding 20 mins)
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity));

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return LocalRedirect(returnUrl);
                }
                return LocalRedirect("/");
            }

            _logger.LogWarning("Login failed for user: {Username} - Invalid credentials", username);
            return Redirect($"/login?error=Credenciales invalidas");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred during login for user: {Username}", username);
            return Redirect($"/login?error=Ocurrio un error: {ex.Message}");
        }
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok();
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Redirect("/login");
    }
}
