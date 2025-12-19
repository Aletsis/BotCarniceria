using BotCarniceria.Core.Application.Interfaces;
using System.Net.Http.Json;

namespace BotCarniceria.Presentation.API.Services;

public class HttpRealTimeNotificationService : IRealTimeNotificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpRealTimeNotificationService> _logger;
    private readonly string _blazorUrl;

    public HttpRealTimeNotificationService(
        IHttpClientFactory httpClientFactory, 
        ILogger<HttpRealTimeNotificationService> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _blazorUrl = configuration["BlazorAppUrl"] ?? "http://localhost:5014";
    }

    public async Task NotifyNewMessageAsync(string phoneNumber, string message)
    {
        try
        {
            var payload = new { PhoneNumber = phoneNumber, Message = message };
            var response = await _httpClient.PostAsJsonAsync($"{_blazorUrl}/api/internal/notifications/message", payload);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to notify Blazor app: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error notifying Blazor app via HTTP");
        }
    }

    public Task NotifyOrdersUpdatedAsync() => Task.CompletedTask;
    public Task NotifySessionExpiredAsync(string phoneNumber) => Task.CompletedTask;
    public Task NotifyOrderPrintedAsync(string orderId) => Task.CompletedTask;
    public Task NotifyUserTypingAsync(string phoneNumber, bool isTyping) => Task.CompletedTask;
    public Task NotifyOrderPickedUpAsync(string orderId) => Task.CompletedTask;
}
