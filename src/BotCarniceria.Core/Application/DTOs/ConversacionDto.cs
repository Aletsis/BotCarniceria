using BotCarniceria.Core.Domain.Entities;

namespace BotCarniceria.Core.Application.DTOs;

public class ConversacionDto
{
    public string NumeroTelefono { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public string? Buffer { get; set; }
    public string? NombreTemporal { get; set; }
    public DateTime UltimaActividad { get; set; }
    public int TimeoutEnMinutos { get; set; }
    
    public bool EstaExpirada { get; set; }
    
    public static ConversacionDto FromEntity(Conversacion conversacion)
    {
        return new ConversacionDto
        {
            NumeroTelefono = conversacion.NumeroTelefono,
            Estado = conversacion.Estado.ToString(),
            Buffer = conversacion.Buffer,
            NombreTemporal = conversacion.NombreTemporal,
            UltimaActividad = conversacion.UltimaActividad,
            TimeoutEnMinutos = conversacion.TimeoutEnMinutos,
            EstaExpirada = conversacion.EstaExpirada()
        };
    }
}
