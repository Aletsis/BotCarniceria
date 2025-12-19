using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Common;

namespace BotCarniceria.Core.Domain.Entities;

public class Configuracion : BaseEntity
{
    public int ConfigID { get; private set; }
    public string Clave { get; private set; } = string.Empty;
    public string Valor { get; private set; } = string.Empty;
    public TipoConfiguracion Tipo { get; private set; }
    public string? Descripcion { get; private set; }
    public bool Editable { get; private set; }
    public DateTime FechaModificacion { get; private set; }

    private Configuracion() { }

    public static Configuracion Create(string clave, string valor, TipoConfiguracion tipo, string? descripcion, bool editable = true)
    {
        return new Configuracion
        {
            Clave = clave,
            Valor = valor,
            Tipo = tipo,
            Descripcion = descripcion,
            Editable = editable,
            FechaModificacion = DateTime.UtcNow
        };
    }

    public void ActualizarValor(string nuevoValor)
    {
        Valor = nuevoValor;
        FechaModificacion = DateTime.UtcNow;
    }
}
