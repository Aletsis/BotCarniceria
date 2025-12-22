using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Events;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace BotCarniceria.Core.Application.DomainEventHandlers;

public class SolicitudFacturaCreadaEventHandler : INotificationHandler<SolicitudFacturaCreadaDomainEvent>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public SolicitudFacturaCreadaEventHandler(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task Handle(SolicitudFacturaCreadaDomainEvent notification, CancellationToken cancellationToken)
    {
        // Fire and Forget notification
        // We capture the ID and pass it to the background thread
        // We cannot use the 'notification.Solicitud' entity directly if it was tracked by the previous context
        // But since we are creating a new scope and fetching fresh data, we just need the IDs.
        
        var solicitudId = notification.Solicitud.SolicitudFacturaID;
        var clienteId = notification.Solicitud.ClienteID;

        _ = Task.Run(async () => await NotifySupervisorsBackground(clienteId, solicitudId));

        return Task.CompletedTask;
    }

    private async Task NotifySupervisorsBackground(int clienteId, long solicitudId)
    {
        try 
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

            // Re-fetch entities in the new scope to ensure context validity
            // Note: Since the event is dispatched "Pre-Commit" in the Infrastructure logic we saw earlier,
            // there is a risk that GetByIdAsync returns null if the transaction hasn't committed yet.
            // HOWEVER, creating a new scope means a NEW DbContext.
            // A new DbContext cannot see uncommitted changes from another transaction (Isolation Level ReadCommitted usually).
            // SO: This background thread MUST wait or retry until the data is visible.
            
            // Retrying logic for eventual consistency
            SolicitudFactura? solicitud = null;
            Cliente? cliente = null;
            int retries = 0;
            while (retries < 5 && (solicitud == null || cliente == null))
            {
                solicitud = await unitOfWork.SolicitudesFactura.GetByIdAsync(solicitudId);
                cliente = await unitOfWork.Clientes.GetByIdAsync(clienteId);
                
                if (solicitud == null || cliente == null)
                {
                    await Task.Delay(500); // Wait for commit to propagate
                    retries++;
                }
            }

            if (cliente == null || solicitud == null) 
            {
                Console.WriteLine($"[SolicitudFacturaCreadaEventHandler] Background error: Client {clienteId} or Solicitud {solicitudId} not found after retries.");
                return;
            }

            var spec = new SupervisorsWithPhoneSpecification();
            var recipients = await unitOfWork.Users.FindAsync(spec);

            var data = solicitud.DatosFacturacion;
            if (data == null) return;

            var regimenDesc = SatCatalogs.RegimenesFiscales.TryGetValue(data.RegimenFiscal ?? "", out var rName) 
                ? $"{data.RegimenFiscal} - {rName}" 
                : data.RegimenFiscal;

            var cfdiDesc = SatCatalogs.UsosCfdi.TryGetValue(solicitud.UsoCFDI ?? "", out var cName)
                ? $"{solicitud.UsoCFDI} - {cName}"
                : solicitud.UsoCFDI;

            var message = "🔔 *Nueva Solicitud de Factura*\n\n" +
                          $"👤 *Cliente:* {cliente.Nombre} ({cliente.NumeroTelefono})\n\n" +
                          "🧾 *Datos de Facturación:*\n" +
                          $"🏢 Razón Social: {data.RazonSocial}\n" +
                          $"🆔 RFC: {data.RFC}\n" +
                          $"📍 Calle: {data.Calle}\n" +
                          $"🔢 Número: {data.Numero}\n" +
                          $"🏘️ Colonia: {data.Colonia}\n" +
                          $"📮 CP: {data.CodigoPostal}\n" +
                          $"📧 Correo: {data.Correo}\n" +
                          $"📑 Régimen: {regimenDesc}\n\n" +
                          "🛒 *Detalles de la Compra:*\n" +
                          $"🧾 Folio Nota: {solicitud.Folio}\n" +
                          $"💲 Total: {solicitud.Total:C2}\n" +
                          $"📄 Uso CFDI: {cfdiDesc}";

            Console.WriteLine($"[SolicitudFacturaCreadaEventHandler] Sending notifications to {recipients.Count} supervisors.");

            foreach (var recipient in recipients)
            {
                if (!string.IsNullOrEmpty(recipient.Telefono))
                {
                     Console.WriteLine($"[SolicitudFacturaCreadaEventHandler] Background sending to {recipient.Telefono}");
                     await whatsAppService.SendTextMessageAsync(recipient.Telefono, message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SolicitudFacturaCreadaEventHandler] Error inside NotifySupervisorsBackground: {ex}");
        }
    }
}
