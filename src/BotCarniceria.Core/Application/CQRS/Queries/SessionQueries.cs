using BotCarniceria.Core.Application.DTOs;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Queries;

public class GetSessionByPhoneQuery : IRequest<ConversacionDto?>
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class IsSessionExpiredQuery : IRequest<bool>
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class GetActiveChatsQuery : IRequest<List<ChatSummaryDto>>
{
}

public class GetChatMessagesQuery : IRequest<List<MensajeDto>>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public int Skip { get; set; } = 0;
    public int Take { get; set; } = 50;
}

public class GetAllConversationsQuery : IRequest<List<ConversacionDto>>
{
}
