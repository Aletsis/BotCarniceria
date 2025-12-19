using BotCarniceria.Core.Domain.Common;
using BotCarniceria.Core.Domain.ValueObjects;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Events;
using BotCarniceria.Core.Domain.Exceptions;

namespace BotCarniceria.Core.Domain.Entities;

public class Pedido : BaseEntity
{
    public long PedidoID { get; private set; }
    public int ClienteID { get; private set; }
    public Folio Folio { get; private set; } = null!;
    public string Contenido { get; private set; } = string.Empty;
    public EstadoPedido Estado { get; private set; }
    public DateTime Fecha { get; private set; }
    public string? Notas { get; private set; }
    public string? FormaPago { get; private set; } // Puede ser un VO o Enum en futuro
    public bool EstadoImpresion { get; private set; }
    public DateTime? FechaImpresion { get; private set; }

    // Navigation property
    public Cliente Cliente { get; private set; } = null!;

    private Pedido() { } // Para EF Core

    public static Pedido Create(int clienteId, string contenido, string? notas = null, string? formaPago = null)
    {
        var pedido = new Pedido
        {
            ClienteID = clienteId,
            Folio = Folio.Generate(),
            Contenido = contenido,
            Estado = EstadoPedido.EnEspera,
            Fecha = DateTime.UtcNow,
            Notas = notas,
            FormaPago = formaPago ?? "Efectivo",
            EstadoImpresion = false
        };
        
        pedido.AddDomainEvent(new PedidoCreatedEvent(pedido));
        
        return pedido;
    }

    public void CambiarEstado(EstadoPedido nuevoEstado)
    {
        if (Estado == EstadoPedido.Entregado && nuevoEstado != EstadoPedido.Entregado)
        {
            throw new InvalidDomainOperationException("No se puede modificar un pedido que ya ha sido entregado.");
        }

        Estado = nuevoEstado;
        // AddDomainEvent(...)
    }

    public void MarcarImpreso()
    {
        EstadoImpresion = true;
        FechaImpresion = DateTime.UtcNow;
    }
}
