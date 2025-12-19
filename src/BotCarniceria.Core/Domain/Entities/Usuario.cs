using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Common;

namespace BotCarniceria.Core.Domain.Entities;

public class Usuario : BaseEntity
{
    public int UsuarioID { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string Nombre { get; private set; } = string.Empty;
    public RolUsuario Rol { get; private set; }
    public bool Activo { get; private set; }
    public DateTime FechaCreacion { get; private set; }
    public DateTime? UltimoAcceso { get; private set; }
    
    // Campo nuevo mencionado en migración 20251129005129_AgregarTelefonoAUsuarios
    public string? Telefono { get; private set; }

    private Usuario() { }

    public static Usuario Create(string username, string passwordHash, string nombre, RolUsuario rol, string? telefono = null)
    {
        if (string.IsNullOrWhiteSpace(username)) throw new ArgumentException("Username requerido");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new ArgumentException("Password hash requerido");

        return new Usuario
        {
            Username = username,
            PasswordHash = passwordHash,
            Nombre = nombre,
            Rol = rol,
            Activo = true,
            FechaCreacion = DateTime.UtcNow,
            Telefono = telefono
        };
    }

    public int AccessFailedCount { get; private set; }
    public DateTime? LockoutEnd { get; private set; }

    public void ActualizarUltimoAcceso()
    {
        UltimoAcceso = DateTime.UtcNow;
    }
    
    public void CambiarPassword(string nuevoHash)
    {
        PasswordHash = nuevoHash;
    }
    
    public void ActualizarDatos(string nombre, string? telefono, RolUsuario rol)
    {
        Nombre = nombre;
        Telefono = telefono;
        Rol = rol;
    }
    
    public void ToggleActivo(bool activo)
    {
        Activo = activo;
    }

    public void RegistrarIntentoFallido()
    {
        AccessFailedCount++;
        if (AccessFailedCount >= 5)
        {
            LockoutEnd = DateTime.UtcNow.AddMinutes(15);
        }
    }

    public void ResetearAccesoFallido()
    {
        AccessFailedCount = 0;
        LockoutEnd = null;
    }

    public bool EstaBloqueado()
    {
        return LockoutEnd.HasValue && LockoutEnd.Value > DateTime.UtcNow;
    }
}
