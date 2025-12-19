using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Commands;

public class ResetSessionCommand : IRequest<bool>
{
    public string PhoneNumber { get; set; } = string.Empty;
}

public class UpdateSessionStateCommand : IRequest<bool>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public string? Buffer { get; set; }
    public string? NombreTemporal { get; set; }
}

public class SendWhatsAppMessageCommand : IRequest<bool>
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CheckSessionTimeoutsCommand : IRequest<Unit>
{
}
