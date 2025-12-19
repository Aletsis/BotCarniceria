using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Common;

namespace BotCarniceria.Core.Domain.Entities;

public class Mensaje : BaseEntity
{
    public long MensajeID { get; private set; }
    public string NumeroTelefono { get; private set; } = string.Empty;
    public TipoMensajeOrigen Origen { get; private set; }
    public string Contenido { get; private set; } = string.Empty;
    public TipoContenidoMensaje TipoContenido { get; private set; }
    public string? MetadataWhatsApp { get; private set; } // JSON string
    public EstadoMensaje Estado { get; private set; }
    public bool FueLeido { get; private set; }
    public DateTime Fecha { get; private set; }
    public string? WhatsAppMessageId { get; private set; } // ID externo de WhatsApp

    private Mensaje() { }

    public static Mensaje CrearEntrante(string telefono, string contenido, TipoContenidoMensaje tipo, string? whatsAppId = null, string? metadata = null)
    {
        return new Mensaje
        {
            NumeroTelefono = telefono,
            Origen = TipoMensajeOrigen.Entrante,
            Contenido = contenido,
            TipoContenido = tipo,
            Estado = EstadoMensaje.Entregado, // Si nos llegó, ya está en servidor
            Fecha = DateTime.UtcNow,
            WhatsAppMessageId = whatsAppId,
            MetadataWhatsApp = metadata,
            FueLeido = false
        };
    }

    public static Mensaje CrearSaliente(string telefono, string contenido, TipoContenidoMensaje tipo)
    {
        return new Mensaje
        {
            NumeroTelefono = telefono,
            Origen = TipoMensajeOrigen.Saliente,
            Contenido = contenido,
            TipoContenido = tipo,
            Estado = EstadoMensaje.Pendiente, // Aún no enviado a API
            Fecha = DateTime.UtcNow,
            FueLeido = true // Lo enviamos nosotros
        };
    }

    public void MarcarComoEnviado(string? whatsAppId = null)
    {
        Estado = EstadoMensaje.Enviado;
        if (whatsAppId != null) WhatsAppMessageId = whatsAppId;
    }

    public void MarcarComoEntregado()
    {
        Estado = EstadoMensaje.Entregado;
    }

    public void MarcarComoLeido()
    {
        Estado = EstadoMensaje.Leido;
        FueLeido = true;
    }
    
    public void MarcarComoFallido(string error)
    {
        Estado = EstadoMensaje.Fallido;
        MetadataWhatsApp = error; // Guardamos el error en metadata por simplicidad
    }

    public void SetMetadata(string metadata)
    {
        MetadataWhatsApp = metadata;
    }
}
