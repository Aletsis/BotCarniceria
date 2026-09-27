using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Models;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class PrinterHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IConfiguracionRepository> _mockSettings;
    private readonly UpdatePrinterSettingsCommandHandler _handler;

    public PrinterHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSettings = new Mock<IConfiguracionRepository>();

        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettings.Object);
        _handler = new UpdatePrinterSettingsCommandHandler(_mockUnitOfWork.Object);
    }

    [Fact]
    public async Task Handle_WithValidSettings_WhenConfigDoesNotExist_CreatesAndSavesNewConfig()
    {
        // Arrange
        var settings = new PrinterSettings
        {
            DefaultPrinterName = "Caja_1",
            Printers = new List<PrinterConfig>
            {
                new() { Name = "Caja_1", IpAddress = "192.168.1.100", Port = 9100 },
                new() { Name = "Cocina_1", IpAddress = "192.168.1.101", Port = 9100 }
            }
        };

        _mockSettings.Setup(x => x.GetByClaveAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync((Configuracion?)null);

        Configuracion? addedConfig = null;
        _mockSettings.Setup(x => x.AddAsync(It.IsAny<Configuracion>()))
            .Callback<Configuracion>(c => addedConfig = c)
            .ReturnsAsync((Configuracion c) => c);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdatePrinterSettingsCommand(settings);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        addedConfig.Should().NotBeNull();
        addedConfig!.Clave.Should().Be(ConfigurationKeys.Printers.Configuration);
        addedConfig.Tipo.Should().Be(TipoConfiguracion.Json);

        var deserialized = JsonSerializer.Deserialize<PrinterSettings>(addedConfig.Valor);
        deserialized.Should().NotBeNull();
        deserialized!.DefaultPrinterName.Should().Be("Caja_1");
        deserialized.Printers.Should().HaveCount(2);

        _mockSettings.Verify(x => x.AddAsync(It.IsAny<Configuracion>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidSettings_WhenConfigExists_UpdatesAndSavesConfig()
    {
        // Arrange
        var existingConfig = Configuracion.Create(
            ConfigurationKeys.Printers.Configuration,
            "{}",
            TipoConfiguracion.Json,
            "Configuracion de Impresoras"
        );

        var settings = new PrinterSettings
        {
            DefaultPrinterName = "Termica_Nueva",
            Printers = new List<PrinterConfig>
            {
                new() { Name = "Termica_Nueva", IpAddress = "192.168.1.200", Port = 9100 }
            }
        };

        _mockSettings.Setup(x => x.GetByClaveAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync(existingConfig);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdatePrinterSettingsCommand(settings);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existingConfig.Valor.Should().Contain("Termica_Nueva");
        _mockSettings.Verify(x => x.UpdateAsync(existingConfig), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithDuplicatePrinterNames_ReturnsFalse()
    {
        // Arrange
        var settings = new PrinterSettings
        {
            DefaultPrinterName = "Printer1",
            Printers = new List<PrinterConfig>
            {
                new() { Name = "EPSON_TM", IpAddress = "192.168.1.100", Port = 9100 },
                new() { Name = "epson_tm", IpAddress = "192.168.1.101", Port = 9100 } // Duplicate case-insensitive
            }
        };

        var command = new UpdatePrinterSettingsCommand(settings);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockSettings.Verify(x => x.AddAsync(It.IsAny<Configuracion>()), Times.Never);
        _mockSettings.Verify(x => x.UpdateAsync(It.IsAny<Configuracion>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDefaultPrinterDoesNotExist_AssignsFirstPrinterAsDefault()
    {
        // Arrange
        var settings = new PrinterSettings
        {
            DefaultPrinterName = "ImpresoraInexistente",
            Printers = new List<PrinterConfig>
            {
                new() { Name = "PrimeraImpresora", IpAddress = "192.168.1.10", Port = 9100 },
                new() { Name = "SegundaImpresora", IpAddress = "192.168.1.11", Port = 9100 }
            }
        };

        _mockSettings.Setup(x => x.GetByClaveAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync((Configuracion?)null);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdatePrinterSettingsCommand(settings);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        settings.DefaultPrinterName.Should().Be("PrimeraImpresora");
    }

    [Fact]
    public async Task Handle_WhenPrintersListIsEmpty_SetsEmptyDefaultPrinterName()
    {
        // Arrange
        var settings = new PrinterSettings
        {
            DefaultPrinterName = "AnteriorDefault",
            Printers = new List<PrinterConfig>()
        };

        _mockSettings.Setup(x => x.GetByClaveAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync((Configuracion?)null);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdatePrinterSettingsCommand(settings);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        settings.DefaultPrinterName.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenSaveChangesReturnsZero_ReturnsFalse()
    {
        // Arrange
        var settings = new PrinterSettings
        {
            DefaultPrinterName = "Caja",
            Printers = new List<PrinterConfig>
            {
                new() { Name = "Caja", IpAddress = "192.168.1.50", Port = 9100 }
            }
        };

        _mockSettings.Setup(x => x.GetByClaveAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync((Configuracion?)null);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new UpdatePrinterSettingsCommand(settings);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }
}
