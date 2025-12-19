using System.Collections.Generic;

namespace BotCarniceria.Core.Domain.ValueObjects;

public class DatosFacturacion : ValueObject
{
    public string RazonSocial { get; private set; }
    public string Calle { get; private set; }
    public string Numero { get; private set; }
    public string Colonia { get; private set; }
    public string CodigoPostal { get; private set; }
    public string Correo { get; private set; }
    public string RegimenFiscal { get; private set; }

    private DatosFacturacion() 
    {
        RazonSocial = string.Empty;
        Calle = string.Empty;
        Numero = string.Empty;
        Colonia = string.Empty;
        CodigoPostal = string.Empty;
        Correo = string.Empty;
        RegimenFiscal = string.Empty;
    }

    public DatosFacturacion(string razonSocial, string calle, string numero, string colonia, string codigoPostal, string correo, string regimenFiscal)
    {
        RazonSocial = razonSocial;
        Calle = calle;
        Numero = numero;
        Colonia = colonia;
        CodigoPostal = codigoPostal;
        Correo = correo;
        RegimenFiscal = regimenFiscal;
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RazonSocial;
        yield return Calle;
        yield return Numero;
        yield return Colonia;
        yield return CodigoPostal;
        yield return Correo;
        yield return RegimenFiscal;
    }
}
