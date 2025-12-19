using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class ConfiguracionHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IConfiguracionRepository> _mockConfigRepository;
    private readonly ConfiguracionHandlers _handler;

    public ConfiguracionHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockConfigRepository = new Mock<IConfiguracionRepository>();
        _mockUnitOfWork.Setup(x => x.Configuraciones).Returns(_mockConfigRepository.Object);
        _handler = new ConfiguracionHandlers(_mockUnitOfWork.Object);
    }

    #region GetAllConfiguracionesQuery Tests

    [Fact]
    public async Task GetAllConfiguracionesQuery_ShouldReturnAllConfiguraciones()
    {
        // Arrange
        var configuraciones = new List<Configuracion>
        {
            Configuracion.Create("timeout_minutes", "30", TipoConfiguracion.Numero, "Timeout en minutos"),
            Configuracion.Create("empresa_nombre", "Carnicería Test", TipoConfiguracion.Texto, "Nombre de la empresa"),
            Configuracion.Create("auto_print", "true", TipoConfiguracion.Booleano, "Impresión automática")
        };

        _mockConfigRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(configuraciones);

        var query = new GetAllConfiguracionesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Clave == "timeout_minutes");
        result.Should().Contain(c => c.Clave == "empresa_nombre");
        result.Should().Contain(c => c.Clave == "auto_print");
    }

    [Fact]
    public async Task GetAllConfiguracionesQuery_WhenNoConfiguraciones_ShouldReturnEmptyList()
    {
        // Arrange
        _mockConfigRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Configuracion>());

        var query = new GetAllConfiguracionesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetConfiguracionByKeyQuery Tests

    [Fact]
    public async Task GetConfiguracionByKeyQuery_WhenConfigExists_ShouldReturnConfigDto()
    {
        // Arrange
        var config = Configuracion.Create("timeout_minutes", "30", TipoConfiguracion.Numero, "Timeout en minutos");
        _mockConfigRepository.Setup(x => x.GetByClaveAsync("timeout_minutes"))
            .ReturnsAsync(config);

        var query = new GetConfiguracionByKeyQuery("timeout_minutes");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Clave.Should().Be("timeout_minutes");
        result.Valor.Should().Be("30");
        result.Tipo.Should().Be(TipoConfiguracion.Numero.ToString());
        result.Descripcion.Should().Be("Timeout en minutos");
    }

    [Fact]
    public async Task GetConfiguracionByKeyQuery_WhenConfigNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockConfigRepository.Setup(x => x.GetByClaveAsync(It.IsAny<string>()))
            .ReturnsAsync((Configuracion?)null);

        var query = new GetConfiguracionByKeyQuery("nonexistent_key");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateConfiguracionCommand Tests

    [Fact]
    public async Task UpdateConfiguracionCommand_ShouldUpdateValueSuccessfully()
    {
        // Arrange
        var config = Configuracion.Create("timeout_minutes", "30", TipoConfiguracion.Numero, "Timeout");
        _mockConfigRepository.Setup(x => x.GetByClaveAsync("timeout_minutes"))
            .ReturnsAsync(config);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateConfiguracionCommand("timeout_minutes", "60");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        config.Valor.Should().Be("60");
        _mockConfigRepository.Verify(x => x.UpdateAsync(It.IsAny<Configuracion>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateConfiguracionCommand_WhenConfigNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockConfigRepository.Setup(x => x.GetByClaveAsync(It.IsAny<string>()))
            .ReturnsAsync((Configuracion?)null);

        var command = new UpdateConfiguracionCommand("nonexistent_key", "value");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockConfigRepository.Verify(x => x.UpdateAsync(It.IsAny<Configuracion>()), Times.Never);
    }

    [Fact]
    public async Task UpdateConfiguracionCommand_ShouldUpdateBooleanConfig()
    {
        // Arrange
        var config = Configuracion.Create("auto_print", "false", TipoConfiguracion.Booleano, "Auto print");
        _mockConfigRepository.Setup(x => x.GetByClaveAsync("auto_print"))
            .ReturnsAsync(config);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateConfiguracionCommand("auto_print", "true");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        config.Valor.Should().Be("true");
    }

    [Fact]
    public async Task UpdateConfiguracionCommand_ShouldUpdateTextConfig()
    {
        // Arrange
        var config = Configuracion.Create("empresa_nombre", "Old Name", TipoConfiguracion.Texto, "Company name");
        _mockConfigRepository.Setup(x => x.GetByClaveAsync("empresa_nombre"))
            .ReturnsAsync(config);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateConfiguracionCommand("empresa_nombre", "New Company Name");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        config.Valor.Should().Be("New Company Name");
    }

    #endregion
}
