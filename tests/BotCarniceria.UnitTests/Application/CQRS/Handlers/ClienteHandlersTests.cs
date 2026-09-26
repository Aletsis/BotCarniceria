using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.ValueObjects;
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
    public async Task GetAllClientesQuery_WithSearchTerm_CaseInsensitive_ShouldFilterCorrectly()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551234567", "Juan Pérez", "Calle 1"),
            Cliente.Create("5559876543", "María García", "Calle 2")
        };

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(clientes);

        var query = new GetAllClientesQuery { SearchTerm = "JUAN" };

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
    public async Task GetAllClientesQuery_WithNonMatchingSearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        var clientes = new List<Cliente>
        {
            Cliente.Create("5551234567", "Juan Pérez", "Calle 1"),
            Cliente.Create("5559876543", "María García", "Calle 2")
        };

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(clientes);

        var query = new GetAllClientesQuery { SearchTerm = "Inexistente999" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
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

    #region GetClienteByRFCQuery Tests

    [Fact]
    public async Task GetClienteByRFCQuery_WhenClienteExistsWithMatchingRFC_ShouldReturnClienteDto()
    {
        // Arrange
        var cliente1 = Cliente.Create("5551111111", "Juan Pérez");
        cliente1.UpdateDatosFacturacion(new DatosFacturacion(
            "Juan Perez SA", "XAXX010101000", "Calle 1", "10", "Colonia", "12345", "juan@test.com", "601"));

        var cliente2 = Cliente.Create("5552222222", "María García");
        cliente2.UpdateDatosFacturacion(new DatosFacturacion(
            "Maria Garcia SA", "XEXX010101000", "Calle 2", "20", "Colonia", "12345", "maria@test.com", "601"));

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Cliente> { cliente1, cliente2 });

        // Query with lowercase to verify case-insensitivity
        var query = new GetClienteByRFCQuery { RFC = "xaxx010101000" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.DatosFacturacion.Should().NotBeNull();
        result.DatosFacturacion!.RFC.Should().Be("XAXX010101000");
        result.DatosFacturacion.RazonSocial.Should().Be("Juan Perez SA");
        result.Nombre.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task GetClienteByRFCQuery_WhenClienteHasNoDatosFacturacion_ShouldNotMatchAndReturnNull()
    {
        // Arrange
        var cliente = Cliente.Create("5551111111", "Juan Pérez"); // DatosFacturacion is null
        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Cliente> { cliente });

        var query = new GetClienteByRFCQuery { RFC = "XAXX010101000" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClienteByRFCQuery_WhenNoMatchingRFC_ShouldReturnNull()
    {
        // Arrange
        var cliente = Cliente.Create("5551111111", "Juan Pérez");
        cliente.UpdateDatosFacturacion(new DatosFacturacion(
            "Juan Perez SA", "XAXX010101000", "Calle 1", "10", "Colonia", "12345", "juan@test.com", "601"));

        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Cliente> { cliente });

        var query = new GetClienteByRFCQuery { RFC = "OTHER999999" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetClienteByRFCQuery_WhenNoClientesExist_ShouldReturnNull()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Cliente>());

        var query = new GetClienteByRFCQuery { RFC = "XAXX010101000" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateClienteCommand Tests

    [Fact]
    public async Task CreateClienteCommand_WithValidData_ShouldCreateClienteAndReturnId()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.AddAsync(It.IsAny<Cliente>()))
            .Callback<Cliente>(c => typeof(Cliente).GetProperty(nameof(Cliente.ClienteID))?.SetValue(c, 100))
            .ReturnsAsync((Cliente c) => c);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateClienteCommand
        {
            NumeroTelefono = "5551234567",
            Nombre = "Carlos López",
            Direccion = "Av Principal 456"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(100);
        _mockClienteRepository.Verify(x => x.AddAsync(It.Is<Cliente>(c =>
            c.NumeroTelefono == "5551234567" &&
            c.Nombre == "Carlos López" &&
            c.Direccion == "Av Principal 456")), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task UpdateClienteCommand_WhenSaveChangesReturnsZero_ShouldReturnFalse()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez", "Calle Vieja");
        _mockClienteRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // Nothing affected

        var command = new UpdateClienteCommand(1, "Juan Carlos Pérez", "Calle Nueva 123");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockClienteRepository.Verify(x => x.UpdateAsync(cliente), Times.Once);
    }

    #endregion

    #region UpdateClienteDatosFacturacionCommand Tests

    [Fact]
    public async Task UpdateClienteDatosFacturacionCommand_WhenClienteExists_ShouldUpdateDatosFacturacionAndReturnTrue()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        _mockClienteRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateClienteDatosFacturacionCommand
        {
            ClienteID = 1,
            RazonSocial = "Empresa SA de CV",
            RFC = "XAXX010101000",
            Calle = "Av Reforma",
            Numero = "100",
            Colonia = "Centro",
            CodigoPostal = "06000",
            Correo = "factura@empresa.com",
            RegimenFiscal = "601"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        cliente.DatosFacturacion.Should().NotBeNull();
        cliente.DatosFacturacion!.RazonSocial.Should().Be("Empresa SA de CV");
        cliente.DatosFacturacion.RFC.Should().Be("XAXX010101000");
        cliente.DatosFacturacion.Calle.Should().Be("Av Reforma");
        cliente.DatosFacturacion.Numero.Should().Be("100");
        cliente.DatosFacturacion.Colonia.Should().Be("Centro");
        cliente.DatosFacturacion.CodigoPostal.Should().Be("06000");
        cliente.DatosFacturacion.Correo.Should().Be("factura@empresa.com");
        cliente.DatosFacturacion.RegimenFiscal.Should().Be("601");

        _mockClienteRepository.Verify(x => x.UpdateAsync(cliente), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateClienteDatosFacturacionCommand_WhenClienteNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockClienteRepository.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Cliente?)null);

        var command = new UpdateClienteDatosFacturacionCommand
        {
            ClienteID = 999,
            RazonSocial = "Razon",
            RFC = "RFC",
            Calle = "Calle",
            Numero = "1",
            Colonia = "Col",
            CodigoPostal = "00000",
            Correo = "mail@test.com",
            RegimenFiscal = "601"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockClienteRepository.Verify(x => x.UpdateAsync(It.IsAny<Cliente>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateClienteDatosFacturacionCommand_WhenSaveChangesReturnsZero_ShouldReturnFalse()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        _mockClienteRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new UpdateClienteDatosFacturacionCommand
        {
            ClienteID = 1,
            RazonSocial = "Razon",
            RFC = "RFC",
            Calle = "Calle",
            Numero = "1",
            Colonia = "Col",
            CodigoPostal = "00000",
            Correo = "mail@test.com",
            RegimenFiscal = "601"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockClienteRepository.Verify(x => x.UpdateAsync(cliente), Times.Once);
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

    [Fact]
    public async Task ToggleClienteActivoCommand_WhenSaveChangesReturnsZero_ShouldReturnFalse()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Juan Pérez");
        _mockClienteRepository.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(cliente);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var command = new ToggleClienteActivoCommand(1, false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockClienteRepository.Verify(x => x.UpdateAsync(cliente), Times.Once);
    }

    #endregion
}
