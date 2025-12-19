using System.Collections.Generic;

namespace BotCarniceria.Core.Domain.Constants
{
    public static class SatCatalogs
    {
        public static readonly Dictionary<string, string> RegimenesFiscales = new()
        {
            { "601", "General de Ley Personas Morales" },
            { "603", "Personas Morales con Fines no Lucrativos" },
            { "605", "Sueldos y Salarios e Ingresos Asimilados a Salarios" },
            { "606", "Arrendamiento" },
            { "608", "Demás ingresos" },
            { "612", "Personas Físicas con Actividades Empresariales y Profesionales" },
            { "614", "Ingresos por intereses" },
            { "616", "Sin obligaciones fiscales" },
            { "621", "Incorporación Fiscal" },
            { "626", "Régimen Simplificado de Confianza" }
        };

        public static readonly Dictionary<string, string> UsosCfdi = new()
        {
            { "G01", "Adquisición de mercancías" },
            { "G03", "Gastos en general" },
            { "I01", "Construcciones" },
            { "I02", "Mobiliario y equipo de oficina por inversiones" },
            { "I03", "Equipo de transporte" },
            { "I04", "Equipo de computo y accesorios" },
            { "I05", "Dados, troqueles, moldes, matrices y herramental" },
            { "I06", "Comunicaciones telefónicas" },
            { "I07", "Comunicaciones satelitales" },
            { "I08", "Otra maquinaria y equipo" },
            { "D01", "Honorarios médicos, dentales y gastos hospitalarios" },
            { "D02", "Gastos médicos por incapacidad o discapacidad" },
            { "D03", "Gastos funerales" },
            { "D04", "Donativos" },
            { "D07", "Primas por seguros de gastos médicos" },
            { "D08", "Gastos de transportación escolar obligatoria" },
            { "D10", "Pagos por servicios educativos (colegiaturas)" },
            { "S01", "Sin efectos fiscales" },
            { "CP01", "Pagos" }
        };
    }
}
