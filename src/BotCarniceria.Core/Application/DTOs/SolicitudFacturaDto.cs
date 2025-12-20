namespace BotCarniceria.Core.Application.DTOs;

public class SolicitudFacturaDto
{
    public long SolicitudFacturaID { get; set; }
    public int ClienteID { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public string Folio { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string UsoCFDI { get; set; } = string.Empty;
    public string UsoCFDIDescripcion { get; set; } = string.Empty;
    
    // Datos de facturación
    public string RazonSocial { get; set; } = string.Empty;
    public string RFC { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Colonia { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RegimenFiscal { get; set; } = string.Empty;
    public string RegimenFiscalDescripcion { get; set; } = string.Empty;
    
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaSolicitud { get; set; }
    public DateTime? FechaProcesada { get; set; }
    public string? Notas { get; set; }
}
