using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using BotCarniceria.Infrastructure.BackgroundJobs.Configuration;

namespace BotCarniceria.Presentation.API.Extensions;

/// <summary>
/// Extensiones para configurar Hangfire Dashboard
/// </summary>
public static class HangfireServiceExtensions
{
    /// <summary>
    /// Configura el dashboard de Hangfire con autenticación
    /// </summary>
    public static IApplicationBuilder UseHangfireDashboardWithAuth(
        this IApplicationBuilder app,
        HangfireOptions options)
    {
        if (!options.EnableDashboard)
            return app;

        app.UseHangfireDashboard(options.DashboardPath, new DashboardOptions
        {
            Authorization = new[] { new HangfireAuthorizationFilter() },
            DashboardTitle = "BotCarniceria - Background Jobs",
            StatsPollingInterval = 2000,
            DisplayStorageConnectionString = false
        });

        return app;
    }
}

/// <summary>
/// Filtro de autorización para el dashboard de Hangfire
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Autoriza el acceso al dashboard
    /// TODO: Implementar autorización real basada en roles/claims
    /// </summary>
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // Allow localhost for local development
        var isLocalhost = httpContext.Request.Host.Host == "localhost" || httpContext.Request.Host.Host == "127.0.0.1";
        if (isLocalhost) return true;

        // Basic Auth for remote access
        var header = httpContext.Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrWhiteSpace(header))
        {
            var authValues = System.Net.Http.Headers.AuthenticationHeaderValue.Parse(header);
            if ("Basic".Equals(authValues.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                var parameter = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(authValues.Parameter));
                var parts = parameter.Split(':');
                if (parts.Length > 1)
                {
                    var username = parts[0];
                    var password = parts[1];

                    var config = httpContext.RequestServices.GetService(typeof(Microsoft.Extensions.Configuration.IConfiguration)) as Microsoft.Extensions.Configuration.IConfiguration;
                    var validUser = config?["Hangfire:User"] ?? "admin";
                    var validPass = config?["Hangfire:Pass"] ?? "admin";

                    if (username == validUser && password == validPass)
                    {
                        return true;
                    }
                }
            }
        }

        // Return 401 to trigger browser prompt
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Hangfire Dashboard\"");
        return false;
    }
}
