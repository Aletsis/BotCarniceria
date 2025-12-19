namespace BotCarniceria.Core.Domain.Models;

public class PrinterConfig
{
    public string Name { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int Port { get; set; } = 9100;
}

public class PrinterSettings
{
    public string DefaultPrinterName { get; set; } = string.Empty;
    public List<PrinterConfig> Printers { get; set; } = new();
}
