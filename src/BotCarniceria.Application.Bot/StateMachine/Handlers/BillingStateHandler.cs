using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Constants;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class BillingStateHandler : IConversationStateHandler
{
    private readonly IWhatsAppService _whatsAppService;
    private readonly IUnitOfWork _unitOfWork;

    public BillingStateHandler(IWhatsAppService whatsAppService, IUnitOfWork unitOfWork)
    {
        _whatsAppService = whatsAppService;
        _unitOfWork = unitOfWork;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, Conversacion session)
    {
        var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
        if (cliente == null)
        {
            await _whatsAppService.SendTextMessageAsync(phoneNumber, "Error: Cliente no encontrado.");
            session.CambiarEstado(ConversationState.MENU);
            return;
        }

        switch (session.Estado)
        {
            case ConversationState.BILLING_WARNING:
                if (messageContent == "billing_warning_continue")
                {
                    // User accepted the warning, proceed with billing flow
                    if (cliente.DatosFacturacion != null && !string.IsNullOrEmpty(cliente.DatosFacturacion.RazonSocial))
                    {
                        // Show existing data for confirmation
                        await ShowConfirmation(phoneNumber, cliente, session);
                    }
                    else
                    {
                        // Ask for billing data
                        await _whatsAppService.SendTextMessageAsync(phoneNumber, "🧾 *Solicitud de Factura*\n\nPara generar tu factura, necesito los siguientes datos fiscales.\n\nPor favor, ingresa tu *Razón Social* (Nombre de la empresa o persona física):");
                        session.CambiarEstado(ConversationState.BILLING_ASK_RAZON_SOCIAL);
                    }
                }
                else if (messageContent == "billing_warning_cancel")
                {
                    // User cancelled, return to menu
                    await _whatsAppService.SendTextMessageAsync(phoneNumber, "De acuerdo, tu solicitud de factura ha sido cancelada. 👍\n\nEscribe 'Hola' para volver al menú principal.");
                    session.CambiarEstado(ConversationState.MENU);
                }
                else
                {
                    // Invalid response, show warning again
                    await _whatsAppService.SendTextMessageAsync(phoneNumber, "Por favor, selecciona una opción válida.");
                }
                break;

            case ConversationState.BILLING_ASK_RAZON_SOCIAL:
                var newRazon = messageContent;
                UpdateBillingData(cliente, razonSocial: newRazon);
                await _unitOfWork.SaveChangesAsync();

                await _whatsAppService.SendTextMessageAsync(phoneNumber, "🏢 *Calle:*\nPor favor, ingresa la calle:");
                session.CambiarEstado(ConversationState.BILLING_ASK_CALLE);
                break;

            case ConversationState.BILLING_ASK_CALLE:
                UpdateBillingData(cliente, calle: messageContent);
                await _unitOfWork.SaveChangesAsync();
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "🔢 *Número:*\nPor favor, ingresa el número exterior/interior:");
                session.CambiarEstado(ConversationState.BILLING_ASK_NUMERO);
                break;

            case ConversationState.BILLING_ASK_NUMERO:
                UpdateBillingData(cliente, numero: messageContent);
                await _unitOfWork.SaveChangesAsync();
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "🏘️ *Colonia:*\nPor favor, ingresa la colonia:");
                session.CambiarEstado(ConversationState.BILLING_ASK_COLONIA);
                break;

            case ConversationState.BILLING_ASK_COLONIA:
                UpdateBillingData(cliente, colonia: messageContent);
                await _unitOfWork.SaveChangesAsync();
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "📮 *Código Postal:*\nPor favor, ingresa el código postal:");
                session.CambiarEstado(ConversationState.BILLING_ASK_CP);
                break;

            case ConversationState.BILLING_ASK_CP:
                UpdateBillingData(cliente, cp: messageContent);
                await _unitOfWork.SaveChangesAsync();
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "📧 *Correo Electrónico:*\nPor favor, ingresa el correo para recibir la factura:");
                session.CambiarEstado(ConversationState.BILLING_ASK_CORREO);
                break;

            case ConversationState.BILLING_ASK_CORREO:
                UpdateBillingData(cliente, correo: messageContent);
                await _unitOfWork.SaveChangesAsync();

                var regimenLabels = SatCatalogs.RegimenesFiscales
                    .Select(kv => (kv.Key, kv.Key, (string?)kv.Value))
                    .ToList();

                await _whatsAppService.SendInteractiveListAsync(
                    phoneNumber, 
                    "📑 *Régimen Fiscal*", 
                    "Ver Regímenes", 
                    regimenLabels, 
                    "Selecciona tu régimen fiscal:", 
                    "Opciones Disponibles"
                );
                
                session.CambiarEstado(ConversationState.BILLING_ASK_REGIMEN);
                break;

            case ConversationState.BILLING_ASK_REGIMEN:
                UpdateBillingData(cliente, regimen: messageContent);
                await _unitOfWork.SaveChangesAsync();
                
                // All data collected. Show confirmation.
                await ShowConfirmation(phoneNumber, cliente, session);
                break;

            case ConversationState.BILLING_CONFIRM_DATA:
                if (messageContent == "billing_confirm")
                {
                    // Proceed to ask invoice details
                    await _whatsAppService.SendTextMessageAsync(phoneNumber, "🧾 *Detalles de la Compra*\n\nPor favor, ingresa el *Folio de la Nota* (ticket):");
                    session.CambiarEstado(ConversationState.BILLING_ASK_NOTE_FOLIO);
                }
                else if (messageContent == "billing_correct")
                {
                     // Restart flow
                     await _whatsAppService.SendTextMessageAsync(phoneNumber, "📝 Vamos a corregir los datos.\n\nPor favor, ingresa tu *Razón Social*:");
                     session.CambiarEstado(ConversationState.BILLING_ASK_RAZON_SOCIAL);
                }
                else
                {
                    await _whatsAppService.SendTextMessageAsync(phoneNumber, "Por favor, selecciona una opción válida.");
                     // Resend buttons?
                     await ShowConfirmation(phoneNumber, cliente, session);
                }
                break;

            case ConversationState.BILLING_ASK_NOTE_FOLIO:
                session.SetFacturaTemp_Folio(messageContent);
                await _unitOfWork.SaveChangesAsync();
                await _whatsAppService.SendTextMessageAsync(phoneNumber, "💲 *Total de la Nota:*\nPor favor, ingresa el monto total del ticket:");
                session.CambiarEstado(ConversationState.BILLING_ASK_NOTE_TOTAL);
                break;

            case ConversationState.BILLING_ASK_NOTE_TOTAL:
                session.SetFacturaTemp_Total(messageContent);
                await _unitOfWork.SaveChangesAsync();

                var cfdiLabels = SatCatalogs.UsosCfdi
                    .Select(kv => (kv.Key, kv.Key, (string?)kv.Value))
                    .ToList();

                await _whatsAppService.SendInteractiveListAsync(
                    phoneNumber, 
                    "📄 *Uso de CFDI*", 
                    "Ver Usos", 
                    cfdiLabels, 
                    "Selecciona el uso CFDI:", 
                    "Usos Disponibles"
                );

                session.CambiarEstado(ConversationState.BILLING_ASK_CFDI);
                break;

            case ConversationState.BILLING_ASK_CFDI:
                session.SetFacturaTemp_UsoCFDI(messageContent);
                await _unitOfWork.SaveChangesAsync();

                // FINALIZATION
                await NotifySupervisors(cliente, session);

                await _whatsAppService.SendTextMessageAsync(phoneNumber, "✅ *Solicitud Recibida*\n\nHemos recibido tu solicitud. Tu factura será enviada en un plazo máximo de 24 hrs. ¡Gracias por tu compra! 🥩");
                
                // Return to menu or start
                session.CambiarEstado(ConversationState.MENU);
                
                // To be nice, send menu again
                // await new MenuStateHandler(_whatsAppService, _unitOfWork).HandleAsync(phoneNumber, "menu_informacion", session); 
                // Better not to manually instantiate, just suggest using /slash command or just the message.
                 // We will send the menu using a helper method if we could access it, or just let them know they can type "menu".
                 await _whatsAppService.SendTextMessageAsync(phoneNumber, "Escribe 'Hola' para volver al menú principal.");
                break;
        }
    }

    private void UpdateBillingData(Cliente cliente, string? razonSocial = null, string? calle = null, string? numero = null, string? colonia = null, string? cp = null, string? correo = null, string? regimen = null)
    {
        var current = cliente.DatosFacturacion;
        
        var newRazon = razonSocial ?? current?.RazonSocial ?? "";
        var newCalle = calle ?? current?.Calle ?? "";
        var newNumero = numero ?? current?.Numero ?? "";
        var newColonia = colonia ?? current?.Colonia ?? "";
        var newCp = cp ?? current?.CodigoPostal ?? "";
        var newCorreo = correo ?? current?.Correo ?? "";
        var newRegimen = regimen ?? current?.RegimenFiscal ?? "";

        var newData = new DatosFacturacion(newRazon, newCalle, newNumero, newColonia, newCp, newCorreo, newRegimen);
        cliente.UpdateDatosFacturacion(newData);
    }

    private async Task ShowConfirmation(string phoneNumber, Cliente cliente, Conversacion session)
    {
         var data = cliente.DatosFacturacion;
         if (data == null) return; 

         var regimenDesc = SatCatalogs.RegimenesFiscales.TryGetValue(data.RegimenFiscal ?? "", out var rName) 
            ? $"{data.RegimenFiscal} - {rName}" 
            : data.RegimenFiscal;

         var msg = "🧾 *Confirma tus Datos de Facturación*\n\n" +
                  $"🏢 *Razón Social:* {data.RazonSocial}\n" +
                  $"📍 *Calle:* {data.Calle}\n" +
                  $"🔢 *Número:* {data.Numero}\n" +
                  $"🏘️ *Colonia:* {data.Colonia}\n" +
                  $"📮 *CP:* {data.CodigoPostal}\n" +
                  $"📧 *Correo:* {data.Correo}\n" +
                  $"📑 *Régimen:* {regimenDesc}\n\n" +
                  "¿Son correctos?";

        var buttons = new List<(string id, string title)>
        {
            ("billing_confirm", "✅ Confirmar"),
            ("billing_correct", "📝 Corregir")
        };
        await _whatsAppService.SendInteractiveButtonsAsync(phoneNumber, msg, buttons);
        session.CambiarEstado(ConversationState.BILLING_CONFIRM_DATA);
    }

    private async Task NotifySupervisors(Cliente cliente, Conversacion session)
    {
        var spec = new SupervisorsWithPhoneSpecification();
        var recipients = await _unitOfWork.Users.FindAsync(spec);

        var data = cliente.DatosFacturacion;
        if (data == null) return;

        var regimenDesc = SatCatalogs.RegimenesFiscales.TryGetValue(data.RegimenFiscal ?? "", out var rName) 
            ? $"{data.RegimenFiscal} - {rName}" 
            : data.RegimenFiscal;

        var cfdiDesc = SatCatalogs.UsosCfdi.TryGetValue(session.FacturaTemp_UsoCFDI ?? "", out var cName)
            ? $"{session.FacturaTemp_UsoCFDI} - {cName}"
            : session.FacturaTemp_UsoCFDI;

        var message = "🔔 *Nueva Solicitud de Factura*\n\n" +
                      $"👤 *Cliente:* {cliente.Nombre} ({cliente.NumeroTelefono})\n\n" +
                      "🧾 *Datos de Facturación:*\n" +
                      $"🏢 Razón Social: {data.RazonSocial}\n" +
                      $"📍 Calle: {data.Calle}\n" +
                      $"🔢 Número: {data.Numero}\n" +
                      $"🏘️ Colonia: {data.Colonia}\n" +
                      $"📮 CP: {data.CodigoPostal}\n" +
                      $"📧 Correo: {data.Correo}\n" +
                      $"📑 Régimen: {regimenDesc}\n\n" +
                      "🛒 *Detalles de la Compra:*\n" +
                      $"🧾 Folio Nota: {session.FacturaTemp_Folio}\n" +
                      $"💲 Total: {session.FacturaTemp_Total}\n" +
                      $"📄 Uso CFDI: {cfdiDesc}";

        foreach (var recipient in recipients)
        {
            if (!string.IsNullOrEmpty(recipient.Telefono))
            {
                 await _whatsAppService.SendTextMessageAsync(recipient.Telefono, message);
            }
        }
    }
}
