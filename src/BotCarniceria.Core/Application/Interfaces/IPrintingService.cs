namespace BotCarniceria.Core.Application.Interfaces;

public interface IPrintingService
{
    Task<bool> PrintTicketAsync(string folio, string nombre, string telefono, string direccion, string contenido, string notas);
}
