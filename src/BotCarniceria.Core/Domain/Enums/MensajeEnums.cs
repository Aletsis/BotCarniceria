namespace BotCarniceria.Core.Domain.Enums;

public enum TipoMensajeOrigen
{
    Entrante = 0, // Recibido del usuario
    Saliente = 1  // Enviado por el bot
}

public enum TipoContenidoMensaje
{
    Texto = 0,
    Interactivo = 1, // Botones, listas
    Imagen = 2,
    Documento = 3,
    Audio = 4,
    Ubicacion = 5,
    Plantilla = 6
}

public enum EstadoMensaje
{
    Pendiente = 0,
    Enviado = 1,
    Entregado = 2,
    Leido = 3,
    Fallido = 4
}
