using BotCarniceria.Core.Domain.Common;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using BotCarniceria.Core.Domain.Events;

namespace BotCarniceria.Core.Domain.Entities;

public class SolicitudFactura : BaseEntity
{
    public long SolicitudFacturaID { get; private set; }
    public int ClienteID { get; private set; }
    public string Folio { get; private set; } = string.Empty;
    public decimal Total { get; private set; }
    public string UsoCFDI { get; private set; } = string.Empty;
    public DatosFacturacion DatosFacturacion { get; private set; } = null!;
    public EstadoSolicitudFactura Estado { get; private set; }
    public DateTime FechaSolicitud { get; private set; }
    public DateTime? FechaProcesada { get; private set; }
    public string? Notas { get; private set; }
    
    // Navigation properties
    public Cliente Cliente { get; private set; } = null!;

    private SolicitudFactura() { } // Para EF Core

    public static SolicitudFactura Create(
        int clienteId,
        string folio,
        decimal total,
        string usoCFDI,
        DatosFacturacion datosFacturacion,
        string? notas = null)
    {
        if (string.IsNullOrWhiteSpace(folio))
            throw new ArgumentException("El folio es requerido", nameof(folio));
        
        if (total <= 0)
            throw new ArgumentException("El total debe ser mayor a cero", nameof(total));
        
        if (string.IsNullOrWhiteSpace(usoCFDI))
            throw new ArgumentException("El uso de CFDI es requerido", nameof(usoCFDI));
        
        if (datosFacturacion == null)
            throw new ArgumentNullException(nameof(datosFacturacion));

        var solicitud = new SolicitudFactura
        {
            ClienteID = clienteId,
            Folio = folio,
            Total = total,
            UsoCFDI = usoCFDI,
            DatosFacturacion = datosFacturacion,
            Estado = EstadoSolicitudFactura.Pendiente,
            FechaSolicitud = DateTime.UtcNow,
            Notas = notas
        };

        solicitud.AddDomainEvent(new SolicitudFacturaCreadaDomainEvent(solicitud));

        return solicitud;
    }

    public void CambiarEstado(EstadoSolicitudFactura nuevoEstado)
    {
        Estado = nuevoEstado;
        
        if (nuevoEstado == EstadoSolicitudFactura.Completada || 
            nuevoEstado == EstadoSolicitudFactura.Rechazada)
        {
            FechaProcesada = DateTime.UtcNow;
        }
    }

    public void ActualizarNotas(string notas)
    {
        Notas = notas;
    }

    public void ActualizarDatosFacturacion(DatosFacturacion nuevosDatos)
    {
        DatosFacturacion = nuevosDatos ?? throw new ArgumentNullException(nameof(nuevosDatos));
    }
}
