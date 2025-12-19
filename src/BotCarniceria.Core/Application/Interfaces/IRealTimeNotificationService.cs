using System.Threading.Tasks;

namespace BotCarniceria.Core.Application.Interfaces;

public interface IRealTimeNotificationService
{
    Task NotifyNewMessageAsync(string phoneNumber, string message);
    Task NotifyOrdersUpdatedAsync();
    
    // New methods
    Task NotifySessionExpiredAsync(string phoneNumber);
    Task NotifyOrderPrintedAsync(string folio);
    Task NotifyUserTypingAsync(string phoneNumber, bool isTyping);
    Task NotifyOrderPickedUpAsync(string folio);
}
