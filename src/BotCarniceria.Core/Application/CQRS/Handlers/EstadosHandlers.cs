using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

public class EstadosHandlers : IRequestHandler<UploadEstadoCommand, string?>
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<EstadosHandlers> _logger;

    public EstadosHandlers(IWhatsAppService whatsAppService, ILogger<EstadosHandlers> logger)
    {
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task<string?> Handle(UploadEstadoCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Iniciando subida de estado: {FileName}", request.FileName);
        
        // Aquí podríamos agregar lógica de dominio:
        // 1. Validar permisos de usuario (aunque ya se hace en UI, el dominio debe protegerse)
        // 2. Registrar en base de datos la subida (Entidad EstadoPublicado)
        // 3. Emitir evento de dominio (EstadoSubidoEvent)
        
        // Por ahora, delegamos al servicio de infraestructura
        var mediaId = await _whatsAppService.UploadMediaAsync(request.FileStream, request.FileName, request.ContentType);

        if (mediaId != null)
        {
            _logger.LogInformation("Estado subido exitosamente. MediaID: {MediaId}", mediaId);
        }
        else
        {
            _logger.LogWarning("Fallo al subir estado");
        }

        return mediaId;
    }
}
