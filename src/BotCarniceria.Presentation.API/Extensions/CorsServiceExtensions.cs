using BotCarniceria.Presentation.API.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace BotCarniceria.Presentation.API.Extensions;

/// <summary>
/// Extension methods for configuring CORS services
/// </summary>
public static class CorsServiceExtensions
{
    private const string PolicyName = "RestrictedCorsPolicy";

    /// <summary>
    /// Adds CORS services with restricted policy based on configuration
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="configuration">The configuration instance</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddRestrictedCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var corsSettings = configuration
            .GetSection("Cors")
            .Get<CorsSettings>();

        var allowedOrigins = corsSettings?.AllowedOrigins ?? Array.Empty<string>();

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policy =>
            {
                if (allowedOrigins.Length > 0)
                {
                    // Restricted policy: only allow specific origins
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials();
                }
                else
                {
                    // Fallback: if no origins configured, allow any origin
                    // but without credentials (safer default)
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Gets the name of the CORS policy
    /// </summary>
    public static string GetPolicyName() => PolicyName;
}
