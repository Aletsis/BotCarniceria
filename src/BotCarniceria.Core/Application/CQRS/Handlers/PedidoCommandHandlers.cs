using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

/// <summary>
/// Handler for creating a new order
/// </summary>
public class CreatePedidoCommandHandler : IRequestHandler<CreatePedidoCommand, PedidoDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreatePedidoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PedidoDto> Handle(CreatePedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = Pedido.Create(request.ClienteID, request.Contenido, request.Notas, request.FormaPago);
        await _unitOfWork.Orders.AddAsync(pedido);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return PedidoDto.FromEntity(pedido);
    }
}

/// <summary>
/// Handler for updating order status with WhatsApp notification
/// </summary>
public class UpdatePedidoEstadoCommandHandler : IRequestHandler<UpdatePedidoEstadoCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;

    public UpdatePedidoEstadoCommandHandler(IUnitOfWork unitOfWork, IWhatsAppService whatsAppService)
    {
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
    }

    public async Task<bool> Handle(UpdatePedidoEstadoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await _unitOfWork.Orders.GetByIdAsync(request.PedidoID);
        if (pedido == null) return false;

        if (Enum.TryParse<EstadoPedido>(request.NuevoEstado, out var nuevoEstado))
        {
            pedido.CambiarEstado(nuevoEstado);
            await _unitOfWork.Orders.UpdateAsync(pedido);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Send WhatsApp notification to customer
            if (pedido.Cliente != null && !string.IsNullOrEmpty(pedido.Cliente.NumeroTelefono))
            {
                var mensaje = GenerarMensajeEstado(pedido, nuevoEstado);
                await _whatsAppService.SendTextMessageAsync(pedido.Cliente.NumeroTelefono, mensaje);
            }

            return true;
        }

        return false;
    }

    private string GenerarMensajeEstado(Pedido pedido, EstadoPedido nuevoEstado)
    {
        var mensaje = $"🔔 *Actualización de tu pedido #{pedido.Folio.Value}*\n\n";

        switch (nuevoEstado)
        {
            case EstadoPedido.EnEspera:
                mensaje += "📦 Tu pedido ha sido recibido y está siendo preparado.\n\n";
                mensaje += "Pronto comenzaremos a surtir tu orden.";
                break;

            case EstadoPedido.EnRuta:
                mensaje += "🚚 ¡Tu pedido está en camino!\n\n";
                mensaje += "Nuestro repartidor está en ruta hacia tu dirección.\n";
                mensaje += $"📍 Dirección de entrega: {pedido.Cliente?.Direccion ?? "Tu dirección"}";
                break;

            case EstadoPedido.Entregado:
                mensaje += "✅ ¡Tu pedido ha sido entregado!\n\n";
                mensaje += "Gracias por tu preferencia. Esperamos que disfrutes tus productos.\n\n";
                mensaje += "¿Necesitas algo más? Escríbenos 'Hola' para hacer un nuevo pedido.";
                break;

            case EstadoPedido.Cancelado:
                mensaje += "❌ Tu pedido ha sido cancelado.\n\n";
                mensaje += "Lamentamos los inconvenientes. Si tienes alguna duda, no dudes en contactarnos.\n\n";
                mensaje += "Escríbenos 'Hola' si deseas hacer un nuevo pedido.";
                break;

            default:
                mensaje += $"Estado actualizado a: {nuevoEstado}";
                break;
        }

        return mensaje;
    }
}

/// <summary>
/// Handler for canceling an order
/// </summary>
public class CancelPedidoCommandHandler : IRequestHandler<CancelPedidoCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public CancelPedidoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(CancelPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await _unitOfWork.Orders.GetByIdAsync(request.PedidoID);
        if (pedido == null) return false;

        pedido.CambiarEstado(EstadoPedido.Cancelado);
        // Could save the reason in notes or event log if needed

        await _unitOfWork.Orders.UpdateAsync(pedido);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}

/// <summary>
/// Handler for printing an order
/// </summary>
public class ImprimirPedidoCommandHandler : IRequestHandler<ImprimirPedidoCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IBackgroundJobService _backgroundJobService;

    public ImprimirPedidoCommandHandler(IUnitOfWork unitOfWork, IBackgroundJobService backgroundJobService)
    {
        _unitOfWork = unitOfWork;
        _backgroundJobService = backgroundJobService;
    }

    public async Task<bool> Handle(ImprimirPedidoCommand request, CancellationToken cancellationToken)
    {
        var pedido = await _unitOfWork.Orders.GetByIdAsync(request.PedidoID);
        if (pedido == null) return false;

        // Mark as printed
        pedido.MarcarImpreso();
        await _unitOfWork.Orders.UpdateAsync(pedido);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Enqueue print job with automatic retry
        try
        {
            var printerName = await _unitOfWork.Settings.GetValorAsync("Printer_Name") ?? "default";
            
            await _backgroundJobService.EnqueueAsync(new EnqueuePrintJob
            {
                PedidoId = pedido.PedidoID,
                PrinterName = printerName,
                PrintDuplicate = false
            }, cancellationToken);

            return true;
        }
        catch
        {
            // Log error but don't fail the command
            // The order is already marked as printed in the database
            // The job queue will handle retries automatically
            return false;
        }
    }
}
