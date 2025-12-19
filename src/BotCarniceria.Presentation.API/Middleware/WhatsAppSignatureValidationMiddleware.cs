using BotCarniceria.Core.Application.Interfaces;
using System.Security.Cryptography;
using System.Text;

namespace BotCarniceria.Presentation.API.Middleware;

/// <summary>
/// Middleware to validate WhatsApp webhook signatures using HMAC-SHA256
/// This ensures that incoming webhooks are actually from WhatsApp
/// </summary>
public class WhatsAppSignatureValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<WhatsAppSignatureValidationMiddleware> _logger;

    public WhatsAppSignatureValidationMiddleware(
        RequestDelegate next,
        ILogger<WhatsAppSignatureValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        // Only apply to WhatsApp webhook POST requests
        if (!context.Request.Path.StartsWithSegments("/api/webhook", StringComparison.OrdinalIgnoreCase) ||
            !context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        // CRITICAL: Enable buffering BEFORE reading the body
        context.Request.EnableBuffering();

        // Read the raw body
        string body;
        using (var reader = new StreamReader(
            context.Request.Body,
            encoding: Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true))
        {
            body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // VERY IMPORTANT: Reset for MVC
        }

        var signature = context.Request.Headers["X-Hub-Signature-256"].ToString();
        
        // Get app secret from database configuration
        var appSecret = await unitOfWork.Settings.GetValorAsync("WhatsApp_AppSecret");

        _logger.LogInformation("=== WhatsApp Webhook Validation ===");
        _logger.LogInformation("Path: {Path}", context.Request.Path);
        _logger.LogInformation("Payload: {Body}", body);
        _logger.LogInformation("Signature received: {Signature}", signature);

        if (!ValidateSignature(body, signature, appSecret))
        {
            _logger.LogWarning("❌ Invalid HMAC signature for webhook request");
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync("{\"error\":\"Invalid signature\"}");
            return;
        }

        _logger.LogInformation("✅ HMAC signature validated successfully");

        // Continue with the pipeline (MVC can now read the body)
        await _next(context);
    }

    private bool ValidateSignature(string body, string signature, string? secret)
    {
        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("X-Hub-Signature-256 header is empty or not found");
            return false;
        }

        if (string.IsNullOrEmpty(body))
        {
            _logger.LogWarning("Request body is empty");
            return false;
        }

        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("WhatsApp App Secret is not configured in database");
            return false;
        }

        var calculatedSignature = ComputeHmacSha256(body, secret);

        var receivedHash = signature.Replace("sha256=", "", StringComparison.OrdinalIgnoreCase)
                                    .Replace("SHA256=", "", StringComparison.OrdinalIgnoreCase);
        var calculatedHash = calculatedSignature.Replace("SHA256=", "", StringComparison.OrdinalIgnoreCase);

        _logger.LogDebug("Hash received:  {Received}", receivedHash);
        _logger.LogDebug("Hash calculated: {Calculated}", calculatedHash);

        // Mitigation: Timing Attack
        // Use fixed-time comparison on the byte representation of the hex strings
        var receivedBytes = Encoding.UTF8.GetBytes(receivedHash);
        var calculatedBytes = Encoding.UTF8.GetBytes(calculatedHash);

        return CryptographicOperations.FixedTimeEquals(receivedBytes, calculatedBytes);
    }

    private string ComputeHmacSha256(string message, string secret)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secret);
        var messageBytes = Encoding.UTF8.GetBytes(message);

        using var hmac = new HMACSHA256(keyBytes);
        byte[] hashArray = hmac.ComputeHash(messageBytes);
        return $"SHA256={BitConverter.ToString(hashArray).Replace("-", string.Empty)}";
    }
}
