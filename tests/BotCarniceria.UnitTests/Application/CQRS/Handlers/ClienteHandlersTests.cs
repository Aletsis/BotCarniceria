using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class ClienteHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IClienteRepository> _mockClienteRepository;
    private readonly ClienteHandlers _handler;

    public ClienteHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockClienteRepository = new Mock<IClienteRepository>();
        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepository.Object);
        _handler = new ClienteHandlers(_mockUnitOfWork.Object);
    }

    #region GetAllClientesQuery Tests

    [Fact]
    public async Task GetAllClientesQuery_ShouldReturnAllClientes()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551234567", "Juan Pérez", "Calle 1"),
            Cliente.Create("5559876543", "María García", "Calle 2"),
            Cliente.Create("5555555555", "Pedro López", "Calle 3")
        };

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(clientes);

        var query = new GetAllClientesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.Nombre == "Juan Pérez");
        result.Should().Contain(c => c.Nombre == "María García");
        result.Should().Contain(c => c.Nombre == "Pedro López");
    }

    [Fact]
    public async Task GetAllClientesQuery_WithSearchTerm_ShouldFilterByNombre()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551234567", "Juan Pérez", "Calle 1"),
            Cliente.Create("5559876543", "María García", "Calle 2"),
            Cliente.Create("5555555555", "Pedro López", "Calle 3")
        };

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(clientes);

        var query = new GetAllClientesQuery { SearchTerm = "Juan" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task GetAllClientesQuery_WithSearchTerm_ShouldFilterByTelefono()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551234567", "Juan Pérez", "Calle 1"),
            Cliente.Create("5559876543", "María García", "Calle 2")
        };

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(clientes);

        var query = new GetAllClientesQuery { SearchTerm = "555123" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().NumeroTelefono.Should().Be("5551234567");
    }

    [Fact]
    public async Task GetAllClientesQuery_WhenNoClientes_ShouldReturnEmptyList()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Cliente>());

        var query = new GetAllClientesQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetClienteByIdQuery Tests

    [Fact]
    public async Task GetClienteByIdQuery_WhenClienteExists_ShouldReturnClienteDto()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle Principal");
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(cliente);

        var query = new GetClienteByIdQuery(1);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Nombre.Should().Be("Juan Pérez");
        result.NumeroTelefono.Should().Be("5551234567");
        result.Direccion.Should().Be("Calle Principal");
    }

    [Fact]
    public async Task GetClienteByIdQuery_WhenClienteNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Cliente?)null);

        var query = new GetClienteByIdQuery(999);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateClienteCommand Tests

    [Fact]
    public async Task UpdateClienteCommand_ShouldUpdateClienteSuccessfully()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle Vieja");
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateClienteCommand(1, "Juan Carlos Pérez", "Calle Nueva 123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        cliente.Nombre.Should().Be("Juan Carlos Pérez");
        cliente.Direccion.Should().Be("Calle Nueva 123");
        _mockClienteRepository.Verify(x => x.UpdateAsync(It.IsAny<Cliente>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateClienteCommand_WhenClienteNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Cliente?)null);

        var command = new UpdateClienteCommand(999, "Test", "Test");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockClienteRepository.Verify(x => x.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    #endregion

    #region ToggleClienteActivoCommand Tests

    [Fact]
    public async Task ToggleClienteActivoCommand_ShouldActivateCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        cliente.ToggleActivo(false); // Desactivar primero
        
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleClienteActivoCommand(1, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        cliente.Activo.Should().BeTrue();
        _mockClienteRepository.Verify(x => x.UpdateAsync(It.IsAny<Cliente>()), Times.Once);
    }

    [Fact]
    public async Task ToggleClienteActivoCommand_ShouldDeactivateCliente()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ToggleClienteActivoCommand(1, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        cliente.Activo.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleClienteActivoCommand_WhenClienteNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Cliente?)null);

        var command = new ToggleClienteActivoCommand(999, true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockClienteRepository.Verify(x => x.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
    }

    #endregion
}
