using Bunit;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Domain.Models;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using System.Text.Json;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class PrinterConfigEditorTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public PrinterConfigEditorTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockSnackbar = new Mock<ISnackbar>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddSingleton(_mockMediator.Object);
        Context.Services.AddSingleton(_mockSnackbar.Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    private ConfiguracionDto CreateSampleConfig()
    {
        var settings = new PrinterSettings
        {
            DefaultPrinterName = "Termica-Caja1",
            Printers = new List<PrinterConfig>
            {
                new PrinterConfig { Name = "Termica-Caja1", IpAddress = "192.168.1.100", Port = 9100 },
                new PrinterConfig { Name = "Termica-Cocina", IpAddress = "192.168.1.101", Port = 9100 }
            }
        };

        return new ConfiguracionDto
        {
            ConfigID = 1,
            Clave = "Impresora",
            Valor = JsonSerializer.Serialize(settings),
            Descripcion = "Configuración de impresoras térmicas"
        };
    }

    [Fact]
    public void PrinterConfigEditor_ShouldRenderConfiguredPrinters()
    {
        // Arrange
        var config = CreateSampleConfig();

        // Act
        var cut = Context.Render<PrinterConfigEditor>(p =>
        {
            p.Add(x => x.Config, config);
        });

        // Assert
        cut.Markup.Should().Contain("Administración de Impresoras");
        cut.Markup.Should().Contain("Termica-Caja1");
        cut.Markup.Should().Contain("192.168.1.100");
        cut.Markup.Should().Contain("Termica-Cocina");
        cut.Markup.Should().Contain("192.168.1.101");
        cut.Markup.Should().Contain("Si"); // Default printer chip
    }

    [Fact]
    public void PrinterConfigEditor_WhenNoConfig_ShouldRenderEmptyTable()
    {
        // Act
        var cut = Context.Render<PrinterConfigEditor>(p =>
        {
            p.Add(x => x.Config, new ConfiguracionDto { Clave = "Impresora", Valor = "" });
        });

        // Assert
        cut.Markup.Should().Contain("No hay impresoras configuradas.");
    }

    [Fact]
    public async Task PrinterConfigEditor_SaveConfig_Success_ShouldCallMediatorAndTriggerCallback()
    {
        // Arrange
        var config = CreateSampleConfig();
        bool callbackCalled = false;

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdatePrinterSettingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Context.Render<PrinterConfigEditor>(p =>
        {
            p.Add(x => x.Config, config);
            p.Add(x => x.OnConfigSaved, () => { callbackCalled = true; });
        });

        // Act
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Guardar Cambios"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockMediator.Verify(m => m.Send(It.IsAny<UpdatePrinterSettingsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("Configuración de impresoras guardada", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
        callbackCalled.Should().BeTrue();
    }

    [Fact]
    public async Task PrinterConfigEditor_SaveConfig_Failure_ShouldShowErrorSnackbar()
    {
        // Arrange
        var config = CreateSampleConfig();

        _mockMediator.Setup(m => m.Send(It.IsAny<UpdatePrinterSettingsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var cut = Context.Render<PrinterConfigEditor>(p =>
        {
            p.Add(x => x.Config, config);
        });

        // Act
        var saveButton = cut.FindAll("button").First(b => b.TextContent.Contains("Guardar Cambios"));
        await cut.InvokeAsync(() => saveButton.Click());

        // Assert
        _mockSnackbar.Verify(s => s.Add("Error al guardar la configuración", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task PrinterConfigEditor_SetDefault_ShouldUpdateDefaultPrinter()
    {
        // Arrange
        var config = CreateSampleConfig();
        var cut = Context.Render<PrinterConfigEditor>(p => p.Add(x => x.Config, config));

        // Act - Click set default button for Termica-Cocina
        var setDefaultButton = cut.FindComponents<MudIconButton>().First(b => b.Instance.Icon == Icons.Material.Outlined.CheckCircle);
        await cut.InvokeAsync(() => setDefaultButton.Find("button").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            var rows = cut.FindAll("tr");
            var cocinaRow = rows.First(r => r.TextContent.Contains("Termica-Cocina"));
            cocinaRow.TextContent.Should().Contain("Si");
        });
    }

    [Fact]
    public async Task PrinterConfigEditor_EditAndCancelEdit_ShouldToggleEditingState()
    {
        // Arrange
        var config = CreateSampleConfig();
        var cut = Context.Render<PrinterConfigEditor>(p => p.Add(x => x.Config, config));

        // Act - Click Edit on first printer
        var editButton = cut.FindComponents<MudIconButton>().First(b => b.Instance.Icon == Icons.Material.Filled.Edit);
        await cut.InvokeAsync(() => editButton.Find("button").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Editar Impresora");
            cut.Markup.Should().Contain("Actualizar");
            cut.Markup.Should().Contain("Cancelar");
        });

        // Act - Click Cancel
        var cancelButton = cut.FindAll("button").First(b => b.TextContent.Contains("Cancelar"));
        await cut.InvokeAsync(() => cancelButton.Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Nueva Impresora");
            cut.Markup.Should().NotContain("Actualizar");
        });
    }

    [Fact]
    public async Task PrinterConfigEditor_RemovePrinter_ShouldRemoveFromTable()
    {
        // Arrange
        var config = CreateSampleConfig();
        var cut = Context.Render<PrinterConfigEditor>(p => p.Add(x => x.Config, config));

        cut.Markup.Should().Contain("Termica-Cocina");

        // Act - Click Delete icon on Termica-Cocina (second printer)
        var deleteButtons = cut.FindComponents<MudIconButton>().Where(b => b.Instance.Icon == Icons.Material.Filled.Delete).ToList();
        deleteButtons.Should().HaveCount(2);
        await cut.InvokeAsync(() => deleteButtons[1].Find("button").Click());

        // Assert
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().NotContain("Termica-Cocina");
            cut.Markup.Should().Contain("Termica-Caja1");
        });
    }
}
