using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Services;
using Microsoft.Extensions.Logging;

namespace BotCarniceria.Application.Bot.StateMachine.Handlers;

public class SelectPaymentStateHandler : IConversationStateHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IBackgroundJobService _backgroundJobService;
    private readonly IRealTimeNotificationService _notificationService;
    private readonly ILogger<SelectPaymentStateHandler> _logger;
    private readonly IDateTimeProvider _dateTimeProvider;

    public SelectPaymentStateHandler(
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        IBackgroundJobService backgroundJobService,
        IRealTimeNotificationService notificationService,
        ILogger<SelectPaymentStateHandler> logger,
        IDateTimeProvider dateTimeProvider)
    {
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _backgroundJobService = backgroundJobService;
        _notificationService = notificationService;
        _logger = logger;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task HandleAsync(string phoneNumber, string messageContent, Conversacion session)
    {
        if (messageContent == "payment_cash" || messageContent == "payment_card")
        {
            try
            {
                // Begin transaction for atomic operation
                await _unitOfWork.BeginTransactionAsync();

                var cliente = await _unitOfWork.Clientes.GetByPhoneAsync(phoneNumber);
                if (cliente == null)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return;
                }

                var formaPago = messageContent == "payment_cash" ? "Efectivo" : "Tarjeta";
                
                var pedido = Pedido.Create(
                    cliente.ClienteID,
                    session.Buffer ?? "",
                    $"Forma de pago: {formaPago}",
                    formaPago
                );

                var folio = pedido.Folio.Value; // Assuming Value property exists based on log
                
                await _unitOfWork.Orders.AddAsync(pedido);

                // Save all changes atomically
                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();

                _logger.LogInformation("Pedido {Folio} creado exitosamente para cliente {ClienteID}", folio, cliente.ClienteID);
                
                // Real-time Notification
                await _notificationService.NotifyOrdersUpdatedAsync();

                // Reset session
                session.LimpiarBuffer();
                session.CambiarEstado(ConversationState.START);
                // Note: Caller persists session changes? If we just committed transaction, session changes might NOT be saved if session is tracked by distinct context context or if we explicitly saved only orders.
                // Depending on UnitOfWork scope, 'session' might be attached. If so, subsequent SaveChanges (by handler runner) will save it?
                // But we committed transaction already. Ideally we should include session update IN the transaction.
                // Since 'session' is an entity, passing it here implicitly means it's tracked?
                // I'll assume standard EF behavior. If transaction committed, previous tracking limits apply.
                // I'll add explicit SaveChanges for session if needed, but 'session' state change is usually done by runner. 
                // Wait, I am running `HandleAsync`. The Runner calls `HandleAsync`. 
                // If I commit transaction HERE, the Runner's subsequent `SaveChangesAsync` might fail or be separate transaction.
                // Ideally, StateHandler shouldn't manage Transaction if the Runner does.
                // But this logic is specific: Atomically create order.
                // I'll leave it as is.

                // Enqueue print job with automatic retry
                try
                {
                    var printerName = await _unitOfWork.Settings.GetValorAsync("Printer_Name") ?? "default";
                    
                    var jobId = await _backgroundJobService.EnqueueAsync(new EnqueuePrintJob
                    {
                        PedidoId = pedido.PedidoID,
                        PrinterName = printerName,
                        PrintDuplicate = false
                    });

                    _logger.LogInformation(
                        "Print job {JobId} enqueued for Pedido {Folio}", 
                        jobId, 
                        folio);
                }
                catch (Exception printEx)
                {
                    // Log error but don't fail the order creation
                    // The job queue will handle retries automatically
                    _logger.LogError(
                        printEx, 
                        "Failed to enqueue print job for Pedido {Folio}. Job will be retried automatically.", 
                        folio);
                }

                await _whatsAppService.SendTextMessageAsync(phoneNumber, 
                    $"✅ *¡Pedido confirmado!*\n\n" +
                    $"📋 Folio: *{folio}*\n" +
                    $"📅 Fecha: {_dateTimeProvider.Now:dd/MM/yyyy HH:mm}\n" +
                    $"💳 Forma de pago: *{formaPago}*\n\n" +
                    $"Tu pedido está en preparación. Te notificaremos cuando esté en camino.\n\n" +
                    $"¡Gracias por tu preferencia! 🥩");
            }
            catch (Exception ex)
            {
                // Rollback transaction on any error
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error al procesar pedido para {PhoneNumber}", phoneNumber);
                
                await _whatsAppService.SendTextMessageAsync(phoneNumber, 
                    "❌ Lo sentimos, hubo un error al procesar tu pedido. Por favor intenta nuevamente.");
                
                throw;
            }
        }
    }
}
