using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;

namespace BotCarniceria.Core.Application.DTOs;

public class PedidoDto
{
    public long PedidoID { get; set; }
    public string Folio { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public int ClienteID { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public string ClienteTelefono { get; set; } = string.Empty;
    public string ClienteDireccion { get; set; } = string.Empty;
    public string Contenido { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public string? FormaPago { get; set; }
    public string? Notas { get; set; }
    public bool EstadoImpresion { get; set; }
    public DateTime? FechaImpresion { get; set; }
    
    public static PedidoDto FromEntity(Pedido pedido)
    {
        return new PedidoDto
        {
            PedidoID = pedido.PedidoID,
            Folio = pedido.Folio.Value,
            Estado = pedido.Estado.ToString(),
            ClienteID = pedido.ClienteID,
            ClienteNombre = pedido.Cliente?.Nombre ?? string.Empty,
            ClienteTelefono = pedido.Cliente?.NumeroTelefono ?? string.Empty,
            ClienteDireccion = pedido.Cliente?.Direccion ?? string.Empty,
            Contenido = pedido.Contenido,
            Fecha = pedido.Fecha,
            FormaPago = pedido.FormaPago,
            Notas = pedido.Notas,
            EstadoImpresion = pedido.EstadoImpresion,
            FechaImpresion = pedido.FechaImpresion
        };
    }
}

public class ClienteDto
{
    public int ClienteID { get; set; }
    public string NumeroTelefono { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string? Direccion { get; set; }
    public bool Activo { get; set; }
    public int TotalPedidos { get; set; }
    public DateTime? UltimoPedidoFecha { get; set; }
    public DatosFacturacionDto? DatosFacturacion { get; set; }
    
    public static ClienteDto FromEntity(Cliente cliente)
    {
        return new ClienteDto
        {
            ClienteID = cliente.ClienteID,
            NumeroTelefono = cliente.NumeroTelefono,
            Nombre = cliente.Nombre,
            Direccion = cliente.Direccion,
            Activo = cliente.Activo,
            TotalPedidos = cliente.Pedidos?.Count ?? 0,
            UltimoPedidoFecha = cliente.Pedidos?.OrderByDescending(p => p.Fecha).FirstOrDefault()?.Fecha,
            DatosFacturacion = cliente.DatosFacturacion != null ? new DatosFacturacionDto
            {
                RazonSocial = cliente.DatosFacturacion.RazonSocial,
                RFC = cliente.DatosFacturacion.RFC,
                Calle = cliente.DatosFacturacion.Calle,
                Numero = cliente.DatosFacturacion.Numero,
                Colonia = cliente.DatosFacturacion.Colonia,
                CodigoPostal = cliente.DatosFacturacion.CodigoPostal,
                Correo = cliente.DatosFacturacion.Correo,
                RegimenFiscal = cliente.DatosFacturacion.RegimenFiscal
            } : null
        };
    }
}

public class DatosFacturacionDto
{
    public string RazonSocial { get; set; } = string.Empty;
    public string RFC { get; set; } = string.Empty;
    public string Calle { get; set; } = string.Empty;
    public string Numero { get; set; } = string.Empty;
    public string Colonia { get; set; } = string.Empty;
    public string CodigoPostal { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RegimenFiscal { get; set; } = string.Empty;
}


public class ConfiguracionDto
{
    public int ConfigID { get; set; }
    public string Clave { get; set; } = string.Empty;
    public string Valor { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Editable { get; set; }
    
    public static ConfiguracionDto FromEntity(Configuracion config)
    {
        return new ConfiguracionDto
        {
            ConfigID = config.ConfigID,
            Clave = config.Clave,
            Valor = config.Valor,
            Tipo = config.Tipo.ToString(),
            Descripcion = config.Descripcion,
            Editable = config.Editable
        };
    }
}

public class UsuarioDto
{
    public int UsuarioID { get; set; }
    public string NombreUsuario { get; set; } = string.Empty;
    public string NombreCompleto { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public bool Activo { get; set; }
    public bool IsLocked { get; set; }
    
    public static UsuarioDto FromEntity(Usuario usuario)
    {
        return new UsuarioDto
        {
            UsuarioID = usuario.UsuarioID,
            NombreUsuario = usuario.Username,
            NombreCompleto = usuario.Nombre,
            Rol = usuario.Rol.ToString(),
            Telefono = usuario.Telefono,
            Activo = usuario.Activo,
            IsLocked = usuario.EstaBloqueado()
        };
    }
}
