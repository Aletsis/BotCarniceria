namespace BotCarniceria.Core.Application.DTOs;

public class DashboardStatsDto
{
    public int PedidosHoy { get; set; }
    public int ChatsActivos { get; set; }
    public decimal IngresosHoy { get; set; } // Assuming we skip total historic for now as it might be heavy and "Total" usually implies recent/relevant unless specified. But user said "Total de ingresos". Let's assume daily for now or minimal MVP. Wait, user said "Estadísticas del día" AND "Total de ingresos". Let's add both.
    public decimal IngresosTotales { get; set; } 
    public List<string> AlertasCriticas { get; set; } = new();
    public List<HourlyActivityDto> ActividadPorHora { get; set; } = new();
}

public class HourlyActivityDto
{
    public string Hora { get; set; } = string.Empty;
    public int CantidadPedidos { get; set; }
    public int CantidadMensajes { get; set; } // Opcional, if we can track it
}
