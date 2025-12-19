namespace BotCarniceria.Presentation.API.Configuration;

/// <summary>
/// Configuration settings for CORS (Cross-Origin Resource Sharing)
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// List of allowed origins for CORS requests
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
