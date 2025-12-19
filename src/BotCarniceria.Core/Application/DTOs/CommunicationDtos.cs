namespace BotCarniceria.Core.Application.DTOs;

public class ChatSummaryDto
{
    public string NumeroTelefono { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty; 
    public string LastMessage { get; set; } = string.Empty;
    public DateTime LastActivity { get; set; }
    public int UnreadCount { get; set; }
}

public class MensajeDto
{
    public long MensajeID { get; set; }
    public string NumeroTelefono { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public bool EsEntrante { get; set; } // True if from user, False if from bot
    public DateTime Fecha { get; set; }
    public bool Leido { get; set; }
    public string Tipo { get; set; } = "text";
    public string? Metadata { get; set; }
}
