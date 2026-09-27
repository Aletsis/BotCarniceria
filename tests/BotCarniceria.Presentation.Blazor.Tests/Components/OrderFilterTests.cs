using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Orders;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Components;

public class OrderFilterTests : IAsyncLifetime
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

    [Fact]
    public void OrderFilter_ShouldRenderWithInitialValues()
    {
        // Act
        var cut = Context.Render<OrderFilter>(parameters =>
        {
            parameters.Add(p => p.SearchString, "Folio-123");
            parameters.Add(p => p.FiltroEstado, "EnEspera");
            parameters.Add(p => p.FechaFiltro, new DateTime(2026, 9, 27));
            parameters.Add(p => p.IsEditor, false);
        });

        // Assert
        cut.Markup.Should().Contain("Folio-123");
        cut.Markup.Should().Contain("Buscar Pedidos");
        cut.Markup.Should().NotContain("Bloqueado para editores");
    }

    [Fact]
    public void OrderFilter_WhenIsEditor_ShouldShowEditorHelperText()
    {
        // Act
        var cut = Context.Render<OrderFilter>(parameters =>
        {
            parameters.Add(p => p.IsEditor, true);
        });

        // Assert
        cut.Markup.Should().Contain("Bloqueado para editores");
    }

    [Fact]
    public void OrderFilter_SearchButtonClick_ShouldInvokeOnSearch()
    {
        // Arrange
        bool searchInvoked = false;
        var cut = Context.Render<OrderFilter>(parameters =>
        {
            parameters.Add(p => p.OnSearch, () => { searchInvoked = true; });
        });

        // Act
        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar"));
        button.Click();

        // Assert
        searchInvoked.Should().BeTrue();
    }

    [Fact]
    public async Task OrderFilter_SearchStringChanged_ShouldFireCallback()
    {
        // Arrange
        string updatedSearch = "";
        var cut = Context.Render<OrderFilter>(parameters =>
        {
            parameters.Add(p => p.SearchString, "");
            parameters.Add(p => p.SearchStringChanged, (string val) => { updatedSearch = val; });
        });

        // Act
        var textField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => textField.Instance.ValueChanged.InvokeAsync("nuevo texto"));

        // Assert
        updatedSearch.Should().Be("nuevo texto");
    }

    [Fact]
    public async Task OrderFilter_FiltroEstadoChanged_ShouldFireCallback()
    {
        // Arrange
        string updatedEstado = "";
        var cut = Context.Render<OrderFilter>(parameters =>
        {
            parameters.Add(p => p.FiltroEstado, "Todos");
            parameters.Add(p => p.FiltroEstadoChanged, (string val) => { updatedEstado = val; });
        });

        // Act - invoke MudSelect change directly on the child component
        var select = cut.FindComponent<MudSelect<string>>();
        await cut.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync("EnRuta"));

        // Assert
        updatedEstado.Should().Be("EnRuta");
    }

    [Fact]
    public async Task OrderFilter_FechaFiltroChanged_ShouldFireCallback()
    {
        // Arrange
        DateTime? updatedFecha = null;
        var cut = Context.Render<OrderFilter>(parameters =>
        {
            parameters.Add(p => p.FechaFiltro, null);
            parameters.Add(p => p.FechaFiltroChanged, (DateTime? val) => { updatedFecha = val; });
        });

        // Act
        var datePicker = cut.FindComponent<MudDatePicker>();
        var testDate = new DateTime(2026, 10, 1);
        await cut.InvokeAsync(() => datePicker.Instance.DateChanged.InvokeAsync(testDate));

        // Assert
        updatedFecha.Should().Be(testDate);
    }
}
