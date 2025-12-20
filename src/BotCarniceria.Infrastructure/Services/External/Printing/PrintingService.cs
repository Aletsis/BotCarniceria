using System.Net.Sockets;
using System.Text;
using BotCarniceria.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Services;

namespace BotCarniceria.Infrastructure.Services.External.Printing;

public class PrintingService : IPrintingService
{
    private readonly ILogger<PrintingService> _logger;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDateTimeProvider _dateTimeProvider;

    public PrintingService(ILogger<PrintingService> logger, IUnitOfWork unitOfWork, IDateTimeProvider dateTimeProvider)
    {
        _logger = logger;
        _unitOfWork = unitOfWork;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<bool> PrintTicketAsync(string folio, string nombre, string telefono, string direccion, string contenido, string notas)
    {
        try 
        {
            var printerIp = "192.168.1.100";
            int printerPort = 9100;

            var printerJson = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.Configuration);
            if (!string.IsNullOrEmpty(printerJson))
            {
                try
                {
                    var settings = System.Text.Json.JsonSerializer.Deserialize<BotCarniceria.Core.Domain.Models.PrinterSettings>(printerJson);
                    if (settings != null && settings.Printers.Any())
                    {
                        var printer = settings.Printers.FirstOrDefault(p => p.Name.Equals(settings.DefaultPrinterName, StringComparison.OrdinalIgnoreCase)) 
                                      ?? settings.Printers.First();
                        printerIp = printer.IpAddress;
                        printerPort = printer.Port;
                    }
                    else
                    {
                        // Fallback to leagacy keys if no printers in list
                         printerIp = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.IpAddress) ?? "192.168.1.100";
                         var printerPortStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.Port) ?? "9100";
                         if (!int.TryParse(printerPortStr, out printerPort)) printerPort = 9100;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error parsing printer configuration.");
                    // Fallback
                    printerIp = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.IpAddress) ?? "192.168.1.100";
                     var printerPortStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.Port) ?? "9100";
                     if (!int.TryParse(printerPortStr, out printerPort)) printerPort = 9100;
                }
            }
            else
            {
                 // Fallback to legacy
                 printerIp = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.IpAddress) ?? "192.168.1.100";
                 var printerPortStr = await _unitOfWork.Settings.GetValorAsync(ConfigurationKeys.Printers.Port) ?? "9100";
                 if (!int.TryParse(printerPortStr, out printerPort)) printerPort = 9100;
            }

            // 1. Build the ticket bytes using TicketBuilder
            var builder = new TicketBuilder()
                .Initialize()
                // Header
                .AlignCenter()
                .BoldOn().WriteLine("BOT CARNICERIA").BoldOff()
                .WriteLine("================================")
                // Order Info
                .AlignLeft()
                .WriteLine($"Folio: {folio}")
                .WriteLine($"Fecha: {_dateTimeProvider.Now:dd/MM/yyyy HH:mm}")
                .WriteSeparator()
                // Customer Info
                .WriteLine($"CLIENTE: {nombre}")
                .WriteLine($"TEL:     {telefono}")
                .WriteLine($"DIR:     {direccion}")
                .WriteSeparator()
                // Content
                .BoldOn().WriteLine("PEDIDO:").BoldOff()
                .WriteLine(contenido)
                .WriteSeparator();

            // Notes
            if (!string.IsNullOrWhiteSpace(notas))
            {
                builder.WriteLine("NOTAS:")
                       .WriteLine(notas)
                       .WriteSeparator();
            }

            // Footer
            builder.AlignCenter()
                   .WriteLine("Gracias por su compra")
                   .FeedAndCut();

            var ticketBytes = builder.Build();

            // 2. Send bytes to printer
            using var client = new TcpClient();
            var connectTask = client.ConnectAsync(printerIp, printerPort);
            var timeoutTask = Task.Delay(2000); // 2 seconds timeout

            if (await Task.WhenAny(connectTask, timeoutTask) == timeoutTask)
            {
                _logger.LogWarning("Timeout connecting to printer at {Ip}:{Port}", printerIp, printerPort);
                return false;
            }
            await connectTask;

            using var stream = client.GetStream();
            await stream.WriteAsync(ticketBytes, 0, ticketBytes.Length);

            _logger.LogInformation("Ticket printed successfully for Folio {Folio}", folio);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error printing ticket {Folio}", folio);
            return false;
        }
    }


}
