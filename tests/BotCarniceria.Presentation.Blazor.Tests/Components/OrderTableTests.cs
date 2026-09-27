using Bunit;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Components.Orders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Components;

public class OrderTableTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.Render<MudPopoverProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    private List<PedidoDto> CreateSampleOrders()
    {
        return new List<PedidoDto>
        {
            new PedidoDto
            {
                PedidoID = 1,
                Folio = "P-100",
                ClienteNombre = "Carlos Ruiz",
                ClienteTelefono = "5511223344",
                Estado = "EnEspera",
                Fecha = new DateTime(2026, 9, 27, 10, 30, 0, DateTimeKind.Utc),
                FormaPago = "Efectivo"
            },
            new PedidoDto
            {
                PedidoID = 2,
                Folio = "P-101",
                ClienteNombre = "Ana Gomez",
                ClienteTelefono = "5522334455",
                Estado = "Entregado",
                Fecha = new DateTime(2026, 9, 27, 11, 15, 0, DateTimeKind.Utc),
                FormaPago = "Tarjeta"
            }
        };
    }

    [Fact]
    public void OrderTable_ShouldRenderOrdersList()
    {
        // Arrange
        var orders = CreateSampleOrders();

        // Act
        var cut = Context.Render<OrderTable>(p =>
        {
            p.Add(x => x.Items, orders);
            p.Add(x => x.TotalItems, 2);
        });

        // Assert
        cut.Markup.Should().Contain("P-100");
        cut.Markup.Should().Contain("Carlos Ruiz");
        cut.Markup.Should().Contain("5511223344");
        cut.Markup.Should().Contain("Efectivo");
        cut.Markup.Should().Contain("P-101");
        cut.Markup.Should().Contain("Ana Gomez");
        cut.Markup.Should().Contain("Tarjeta");
        cut.Markup.Should().Contain("Mostrando 2 de 2 pedidos");
    }

    [Fact]
    public void OrderTable_WhenEmpty_ShouldRenderNoRecordsMessage()
    {
        // Act
        var cut = Context.Render<OrderTable>(p =>
        {
            p.Add(x => x.Items, new List<PedidoDto>());
            p.Add(x => x.TotalItems, 0);
        });

        // Assert
        cut.Markup.Should().Contain("No se encontraron pedidos con los filtros aplicados.");
        cut.Markup.Should().Contain("Mostrando 0 de 0 pedidos");
    }

    [Fact]
    public async Task OrderTable_ClickViewDetails_ShouldInvokeOnViewDetails()
    {
        // Arrange
        var orders = CreateSampleOrders();
        PedidoDto? selectedOrder = null;

        var cut = Context.Render<OrderTable>(p =>
        {
            p.Add(x => x.Items, orders);
            p.Add(x => x.OnViewDetails, (PedidoDto order) => { selectedOrder = order; });
        });

        // Act - Click first view details button
        var viewDetailsButtons = cut.FindAll("button[aria-label='Ver detalles del pedido']");
        viewDetailsButtons.Should().NotBeEmpty();
        await cut.InvokeAsync(() => viewDetailsButtons[0].Click());

        // Assert
        selectedOrder.Should().NotBeNull();
        selectedOrder!.Folio.Should().Be("P-100");
    }

    [Fact]
    public async Task OrderTable_ClickPrint_ShouldInvokeOnPrint()
    {
        // Arrange
        var orders = CreateSampleOrders();
        PedidoDto? printedOrder = null;

        var cut = Context.Render<OrderTable>(p =>
        {
            p.Add(x => x.Items, orders);
            p.Add(x => x.OnPrint, (PedidoDto order) => { printedOrder = order; });
        });

        // Act - Click first print button
        var printButtons = cut.FindAll("button[aria-label='Imprimir comprobante']");
        printButtons.Should().NotBeEmpty();
        await cut.InvokeAsync(() => printButtons[0].Click());

        // Assert
        printedOrder.Should().NotBeNull();
        printedOrder!.Folio.Should().Be("P-100");
    }

    [Fact]
    public async Task OrderTable_ChangeStatus_ShouldInvokeOnStatusChanged()
    {
        // Arrange
        var orders = CreateSampleOrders();
        Tuple<PedidoDto, string>? statusChange = null;

        var cut = Context.Render<OrderTable>(p =>
        {
            p.Add(x => x.Items, orders);
            p.Add(x => x.OnStatusChanged, (Tuple<PedidoDto, string> tuple) => { statusChange = tuple; });
        });

        // Act - Invoke status select ValueChanged on first order
        var select = cut.FindComponents<MudSelect<string>>().First();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync("EnRuta"));

        // Assert
        statusChange.Should().NotBeNull();
        statusChange!.Item1.Folio.Should().Be("P-100");
        statusChange.Item2.Should().Be("EnRuta");
    }
}
