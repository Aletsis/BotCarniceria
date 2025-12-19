using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Common;

namespace BotCarniceria.Core.Domain.Entities;

public class Conversacion : BaseEntity
{
    public string NumeroTelefono { get; private set; } = string.Empty;
    public ConversationState Estado { get; private set; }
    public string? Buffer { get; private set; }
    public string? NombreTemporal { get; private set; }
    public string? FacturaTemp_Folio { get; private set; }
    public string? FacturaTemp_Total { get; private set; }
    public string? FacturaTemp_UsoCFDI { get; private set; }
    public DateTime UltimaActividad { get; private set; }
    public int TimeoutEnMinutos { get; private set; }

    // Propiedades calculada (o persistida si se prefiere)
    public DateTime? FechaExpiracion => UltimaActividad.AddMinutes(TimeoutEnMinutos);

    public bool NotificacionTimeoutEnviada { get; private set; }

    private Conversacion() { }

    public static Conversacion Create(string numeroTelefono, int timeoutMinutes = 30)
    {
        if (string.IsNullOrWhiteSpace(numeroTelefono)) throw new ArgumentException("Teléfono requerido");

        return new Conversacion
        {
            NumeroTelefono = numeroTelefono,
            Estado = ConversationState.START,
            UltimaActividad = DateTime.UtcNow,
            TimeoutEnMinutos = timeoutMinutes,
            NotificacionTimeoutEnviada = false
        };
    }

    public void ActualizarActividad()
    {
        UltimaActividad = DateTime.UtcNow;
        NotificacionTimeoutEnviada = false;
    }

    public void MarcarNotificacionTimeoutEnviada()
    {
        NotificacionTimeoutEnviada = true;
    }

    public void CambiarEstado(ConversationState nuevoEstado)
    {
        Estado = nuevoEstado;
        ActualizarActividad();
    }

    public void GuardarBuffer(string dato)
    {
        Buffer = dato;
        ActualizarActividad();
    }

    public void LimpiarBuffer()
    {
        Buffer = null;
    }
    
    public void GuardarNombreTemporal(string nombre)
    {
        NombreTemporal = nombre;
        ActualizarActividad();
    }

    public void SetFacturaTemp_Folio(string folio) { FacturaTemp_Folio = folio; ActualizarActividad(); }
    public void SetFacturaTemp_Total(string total) { FacturaTemp_Total = total; ActualizarActividad(); }
    public void SetFacturaTemp_UsoCFDI(string uso) { FacturaTemp_UsoCFDI = uso; ActualizarActividad(); }

    public bool EstaExpirada()
    {
        return DateTime.UtcNow > FechaExpiracion;
    }
}
