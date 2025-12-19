using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Application.CQRS.Queries;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

public class DashboardQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        // 1. Pedidos Hoy
        var pedidosHoySpec = new PedidosTodaySpecification();
        var pedidosHoy = await _unitOfWork.Orders.FindAsync(pedidosHoySpec);
        var cantidadPedidosHoy = pedidosHoy.Count;

        // 2. Chats Activos
        var activeChatsSpec = new ActiveSessionsSpecification();
        var cantidadChatsActivos = await _unitOfWork.Sessions.CountAsync(activeChatsSpec);

        // 3. Activity Chart (Orders by Hour)
        var activity = pedidosHoy
            .GroupBy(p => p.Fecha.ToLocalTime().Hour)
            .Select(g => new HourlyActivityDto 
            { 
                Hora = $"{g.Key:00}:00", 
                CantidadPedidos = g.Count() 
            })
            .OrderBy(a => a.Hora)
            .ToList();

        // 4. Alerts
        var alerts = new List<string>();
        
        // Alert: Pedidos pendientes not being printed/handled
        var pendingSpec = new PedidosPendingSpecification();
        var pendingOrders = await _unitOfWork.Orders.FindAsync(pendingSpec);
        if (pendingOrders.Count(p => (DateTime.UtcNow - p.Fecha).TotalMinutes > 15) > 0)
        {
            alerts.Add($"{pendingOrders.Count(p => (DateTime.UtcNow - p.Fecha).TotalMinutes > 15)} pedidos en espera por más de 15 min.");
        }

        // 5. Ingresos (Placeholder as Pedido doesn't have Total amount yet)
        decimal ingresosHoy = 0; // Requires Price/Amount field in Pedido
        decimal ingresosTotales = 0; 

        return new DashboardStatsDto
        {
            PedidosHoy = cantidadPedidosHoy,
            ChatsActivos = cantidadChatsActivos,
            IngresosHoy = ingresosHoy,
            IngresosTotales = ingresosTotales,
            AlertasCriticas = alerts,
            ActividadPorHora = activity
        };
    }
}
