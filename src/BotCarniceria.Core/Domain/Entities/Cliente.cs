using BotCarniceria.Core.Domain.ValueObjects;
using BotCarniceria.Core.Domain.Common;

namespace BotCarniceria.Core.Domain.Entities;

public class Cliente : BaseEntity
{
    public int ClienteID { get; private set; }
    public string NumeroTelefono { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public string? Direccion { get; private set; }
    public DatosFacturacion? DatosFacturacion { get; private set; }
    public DateTime FechaAlta { get; private set; }
    public bool Activo { get; private set; }
    
    // Navigation property
    private readonly List<Pedido> _pedidos = new();
    public IReadOnlyCollection<Pedido> Pedidos => _pedidos.AsReadOnly();

    private Cliente() { } // Para EF Core

    public static Cliente Create(string numeroTelefono, string nombre, string? direccion = null)
    {
        if (string.IsNullOrWhiteSpace(numeroTelefono)) throw new ArgumentException("Teléfono requerido");
        if (string.IsNullOrWhiteSpace(nombre)) throw new ArgumentException("Nombre requerido");

        return new Cliente
        {
            NumeroTelefono = numeroTelefono,
            Nombre = nombre,
            Direccion = direccion,
            FechaAlta = DateTime.UtcNow,
            Activo = true
        };
    }

    public void UpdateDireccion(string nuevaDireccion)
    {
        Direccion = nuevaDireccion;
    }
    
    public void UpdateNombre(string nuevoNombre)
    {
        if (!string.IsNullOrWhiteSpace(nuevoNombre))
        {
            Nombre = nuevoNombre;
        }
    }

    public void ActualizarDatos(string nombre, string? direccion)
    {
        Nombre = nombre;
        Direccion = direccion;
    }

    public void UpdateDatosFacturacion(DatosFacturacion datos)
    {
        DatosFacturacion = datos;
    }

    public void ToggleActivo(bool activo)
    {
        Activo = activo;
    }
}
