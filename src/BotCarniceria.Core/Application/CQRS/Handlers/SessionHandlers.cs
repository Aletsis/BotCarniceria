using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Domain.Constants;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

// --- COMMAND HANDLERS ---

public class ResetSessionCommandHandler : IRequestHandler<ResetSessionCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    public ResetSessionCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(ResetSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByPhoneAsync(request.PhoneNumber);
        
        if (session != null)
        {
            // Reset logic: Maybe create a new session or reset existing one?
            // Assuming reset means setting state to START and clearing buffer
            session.CambiarEstado(ConversationState.START);
            session.LimpiarBuffer();
            session.GuardarNombreTemporal(""); // Clear temp name
            
            await _unitOfWork.Sessions.UpdateAsync(session);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return true;
        }
        return false;
    }
}

public class UpdateSessionStateCommandHandler : IRequestHandler<UpdateSessionStateCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    public UpdateSessionStateCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(UpdateSessionStateCommand request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByPhoneAsync(request.PhoneNumber);
        
        if (session == null)
        {
            // Get configured timeout
            var timeoutStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Session.BotTimeoutMinutes) ?? "30";
            if (!int.TryParse(timeoutStr, out int timeout)) timeout = 30;

            // Create session with configured timeout
            session = Conversacion.Create(request.PhoneNumber, timeout);
            await _unitOfWork.Sessions.AddAsync(session);
        }

        if (Enum.TryParse<ConversationState>(request.NewState, out var newState))
        {
            session.CambiarEstado(newState);
        }
        
        if (request.Buffer != null)
        {
            session.GuardarBuffer(request.Buffer);
        }
        
        if (request.NombreTemporal != null)
        {
            session.GuardarNombreTemporal(request.NombreTemporal);
        }

        // Implicitly updates LastActivity via methods
        
        if (session.NumeroTelefono == request.PhoneNumber) // Just checking existence
        {
             // If it was an existing session, we update. If new, we added it.
             // EF Core tracks it, so SaveChanges will commit Add or Update.
             if (await _unitOfWork.Sessions.GetByPhoneAsync(request.PhoneNumber) != null) // Check if already tracked/persisted
             {
                 await _unitOfWork.Sessions.UpdateAsync(session);
             }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

// --- QUERY HANDLERS ---

public class GetSessionByPhoneQueryHandler : IRequestHandler<GetSessionByPhoneQuery, ConversacionDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetSessionByPhoneQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<ConversacionDto?> Handle(GetSessionByPhoneQuery request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByPhoneAsync(request.PhoneNumber);
        return session == null ? null : ConversacionDto.FromEntity(session);
    }
}

public class IsSessionExpiredQueryHandler : IRequestHandler<IsSessionExpiredQuery, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    public IsSessionExpiredQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(IsSessionExpiredQuery request, CancellationToken cancellationToken)
    {
        var session = await _unitOfWork.Sessions.GetByPhoneAsync(request.PhoneNumber);
        
        if (session == null) return true; // No session = expired/none
        
        return session.EstaExpirada();
    }
}

public class GetActiveChatsQueryHandler : IRequestHandler<GetActiveChatsQuery, List<ChatSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetActiveChatsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<List<ChatSummaryDto>> Handle(GetActiveChatsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _unitOfWork.Sessions.GetAllAsync();
        
        // This is N+1 if we fetch client for each, if not included.
        // Assuming GetAllAsync includes basic props.
        // We'll simplistic map for now.
        
        var summaries = new List<ChatSummaryDto>();
        foreach(var s in sessions.OrderByDescending(x => x.UltimaActividad))
        {
             // Try to find client name
             var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(s.NumeroTelefono);
             
             summaries.Add(new ChatSummaryDto
             {
                 NumeroTelefono = s.NumeroTelefono,
                 Nombre = cliente?.Nombre ?? s.NombreTemporal ?? "Desconocido",
                 LastActivity = s.UltimaActividad,
                 UnreadCount = 0, // Placeholder
                 LastMessage = "..." // Placeholder
             });
        }
        return summaries;
    }
}

public class GetChatMessagesQueryHandler : IRequestHandler<GetChatMessagesQuery, List<MensajeDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetChatMessagesQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;
    
    public async Task<List<MensajeDto>> Handle(GetChatMessagesQuery request, CancellationToken cancellationToken)
    {
        var mensajes = await _unitOfWork.Messages.GetByPhoneAsync(request.PhoneNumber, request.Take, request.Skip);
        return mensajes.Select(m => new MensajeDto
        {
            MensajeID = m.MensajeID,
            NumeroTelefono = m.NumeroTelefono,
            Contenido = m.Contenido,
            EsEntrante = m.Origen == TipoMensajeOrigen.Entrante,
            Fecha = m.Fecha,
            Leido = m.FueLeido,
            Tipo = m.TipoContenido.ToString().ToLower(),
            Metadata = m.MetadataWhatsApp
        }).ToList();
    }
}

public class GetAllConversationsQueryHandler : IRequestHandler<GetAllConversationsQuery, List<ConversacionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    public GetAllConversationsQueryHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<List<ConversacionDto>> Handle(GetAllConversationsQuery request, CancellationToken cancellationToken)
    {
        var sessions = await _unitOfWork.Sessions.GetAllAsync();
        return sessions.OrderByDescending(s => s.UltimaActividad).Select(ConversacionDto.FromEntity).ToList();
    }
}

public class SendWhatsAppMessageCommandHandler : IRequestHandler<SendWhatsAppMessageCommand, bool>
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;
    
    public SendWhatsAppMessageCommandHandler(IWhatsAppService whatsAppService, IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(SendWhatsAppMessageCommand request, CancellationToken cancellationToken)
    {
        try 
        {
             // Note: WhatsAppService already saves to DB in this project's implementation
             // (See WhatsAppService.cs in Infrastructure)
             // So we just call the service.
             
             return await _whatsAppService.SendTextMessageAsync(request.PhoneNumber, request.Message);
        }
        catch 
        {
             return false;
        }
    }
}

public class CheckSessionTimeoutsCommandHandler : IRequestHandler<CheckSessionTimeoutsCommand, Unit>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsappService;
    private readonly Microsoft.Extensions.Logging.ILogger<CheckSessionTimeoutsCommandHandler> _logger;
    private const int WarningMinutesBefore = 2; // Business Rule: configurable

    public CheckSessionTimeoutsCommandHandler(
        IUnitOfWork unitOfWork, 
        IWhatsAppService whatsappService,
        Microsoft.Extensions.Logging.ILogger<CheckSessionTimeoutsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _whatsappService = whatsappService;
        _logger = logger;
    }

    public async Task<Unit> Handle(CheckSessionTimeoutsCommand request, CancellationToken cancellationToken)
    {
        // 1. Get configurations from DB
        var timeoutMinutesStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Session.BotTimeoutMinutes) ?? "30";
        var warningMinutesStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Session.BotWarningMinutes) ?? "2";
        
        int.TryParse(timeoutMinutesStr, out int timeoutMinutes);
        int.TryParse(warningMinutesStr, out int warningMinutes);

        await ProcessExpiringSessionsAsync(warningMinutes, timeoutMinutes, cancellationToken);
        await ProcessExpiring24hSessionsAsync(cancellationToken);
        await ProcessExpiredSessionsAsync(timeoutMinutes, cancellationToken);

        return Unit.Value;
    }

    private async Task ProcessExpiringSessionsAsync(int warningMinutes, int timeoutMinutes, CancellationToken cancellationToken)
    {
        // 1. Notify Expiring Sessions
        var expiringSpec = new BotCarniceria.Core.Application.Specifications.ExpiringSessionsSpecification(warningMinutes, timeoutMinutes);
        var expiringSessions = await _unitOfWork.Sessions.FindAsync(expiringSpec);

        if (expiringSessions.Any())
        {
            _logger.LogInformation("Found {Count} sessions processing expiration warning.", expiringSessions.Count);

            foreach (var session in expiringSessions)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await _whatsappService.SendTextMessageAsync(
                    session.NumeroTelefono,
                    "⚠️ *Aviso de inactividad*\n\nSu sesión está por expirar. Si desea continuar con su pedido, por favor envíe una opción o escriba algo."
                );

                session.MarcarNotificacionTimeoutEnviada();
                await _unitOfWork.Sessions.UpdateAsync(session);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessExpiring24hSessionsAsync(CancellationToken cancellationToken)
    {
        // 1. Get Admins and Supervisors
        var adminSpec = new BotCarniceria.Core.Application.Specifications.AdminsAndSupervisorsWithPhoneSpecification();
        var targetUsers = await _unitOfWork.Users.FindAsync(adminSpec);
        var targetPhones = targetUsers.Select(u => u.Telefono).ToHashSet();

        if (!targetPhones.Any()) return; 

        // 2. Get Sessions expiring in 24h
        var spec = new BotCarniceria.Core.Application.Specifications.Expiring24hSessionsSpecification(23); // Warn at 23h
        var expiringSessions = await _unitOfWork.Sessions.FindAsync(spec);

        // 3. Filter sessions by target phones
        var sessionsToNotify = expiringSessions.Where(s => targetPhones.Contains(s.NumeroTelefono)).ToList();

        if (sessionsToNotify.Any())
        {
            _logger.LogInformation("Found {Count} admin/supervisor sessions closing on 24h limit.", sessionsToNotify.Count);

            foreach (var session in sessionsToNotify)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await _whatsappService.SendTextMessageAsync(
                    session.NumeroTelefono,
                    "⚠️ *Aviso de inactividad prolongada*\n\nEstán por pasar 24 horas desde su último mensaje. Después de este tiempo el bot ya no podrá enviarle notificaciones hasta que usted nos escriba nuevamente."
                );

                session.MarcarNotificacion24hEnviada();
                await _unitOfWork.Sessions.UpdateAsync(session);
            }
            // Save changes once after processing batch
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task ProcessExpiredSessionsAsync(int timeoutMinutes, CancellationToken cancellationToken)
    {
        // 2. Reset Expired Sessions
        var expiredSpec = new BotCarniceria.Core.Application.Specifications.ExpiredSessionsSpecification(timeoutMinutes);
        var expiredSessions = await _unitOfWork.Sessions.FindAsync(expiredSpec);

        if (expiredSessions.Any())
        {
            _logger.LogInformation("Found {Count} sessions expired.", expiredSessions.Count);

            foreach (var session in expiredSessions)
            {
                if (cancellationToken.IsCancellationRequested) break;

                await _whatsappService.SendTextMessageAsync(
                    session.NumeroTelefono,
                    "🕒 *Sesión expirada*\n\nSu sesión ha expirado por inactividad. Gracias por contactarnos. Escriba *Hola* cuando desee iniciar un nuevo pedido."
                );

                session.CambiarEstado(ConversationState.START);
                session.LimpiarBuffer();
                
                await _unitOfWork.Sessions.UpdateAsync(session);
            }
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
