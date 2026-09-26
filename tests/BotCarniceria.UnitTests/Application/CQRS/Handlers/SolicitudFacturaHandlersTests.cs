using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class SolicitudFacturaHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ISolicitudFacturaRepository> _mockSolicitudRepo;
    private readonly Mock<IClienteRepository> _mockClienteRepo;

    public SolicitudFacturaHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSolicitudRepo = new Mock<ISolicitudFacturaRepository>();
        _mockClienteRepo = new Mock<IClienteRepository>();

        _mockUnitOfWork.Setup(x => x.SolicitudesFactura).Returns(_mockSolicitudRepo.Object);
        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClienteRepo.Object);
    }

    private static (Cliente cliente, SolicitudFactura solicitud) CreateTestEntities(
        int clienteId = 1,
        long solicitudId = 100,
        string rfc = "XAXX010101000",
        string regimen = "601",
        string usoCfdi = "G01")
    {
        var cliente = Cliente.Create("5551234567", "Empresa Test");
        typeof(Cliente).GetProperty(nameof(Cliente.ClienteID))?.SetValue(cliente, clienteId);

        var datosFacturacion = new DatosFacturacion(
            "Empresa Test SA de CV", rfc, "Av Central", "123", "Centro", "01000", "contacto@test.com", regimen);
        cliente.UpdateDatosFacturacion(datosFacturacion);

        var solicitud = SolicitudFactura.Create(
            clienteId, "FAC-001", 1500.50m, usoCfdi, datosFacturacion, "Notas de prueba");
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.SolicitudFacturaID))?.SetValue(solicitud, solicitudId);
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.Cliente))?.SetValue(solicitud, cliente);

        return (cliente, solicitud);
    }

    #region CreateSolicitudFacturaCommandHandler Tests

    [Fact]
    public async Task CreateSolicitudFacturaCommandHandler_WhenClientHasBillingData_ShouldCreateSolicitudAndReturnId()
    {
        // Arrange
        var (cliente, _) = CreateTestEntities();
        _mockClienteRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(cliente);

        SolicitudFactura? capturedSolicitud = null;
        _mockSolicitudRepo.Setup(x => x.AddAsync(It.IsAny<SolicitudFactura>()))
            .Callback<SolicitudFactura>(s =>
            {
                capturedSolicitud = s;
                typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.SolicitudFacturaID))?.SetValue(s, 777L);
            })
            .ReturnsAsync((SolicitudFactura s) => s);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new CreateSolicitudFacturaCommand
        {
            ClienteID = 1,
            Folio = "F-2026-001",
            Total = 2500.75m,
            UsoCFDI = "G03",
            Notas = "Facturar urgente"
        };
        var handler = new CreateSolicitudFacturaCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().Be(777L);
        capturedSolicitud.Should().NotBeNull();
        capturedSolicitud!.ClienteID.Should().Be(1);
        capturedSolicitud.Folio.Should().Be("F-2026-001");
        capturedSolicitud.Total.Should().Be(2500.75m);
        capturedSolicitud.UsoCFDI.Should().Be("G03");
        capturedSolicitud.Notas.Should().Be("Facturar urgente");
        capturedSolicitud.Estado.Should().Be(EstadoSolicitudFactura.Pendiente);

        // Verify billing data copied
        capturedSolicitud.DatosFacturacion.RFC.Should().Be(cliente.DatosFacturacion!.RFC);
        capturedSolicitud.DatosFacturacion.RazonSocial.Should().Be(cliente.DatosFacturacion.RazonSocial);
        capturedSolicitud.DatosFacturacion.Calle.Should().Be(cliente.DatosFacturacion.Calle);
        capturedSolicitud.DatosFacturacion.RegimenFiscal.Should().Be(cliente.DatosFacturacion.RegimenFiscal);

        _mockSolicitudRepo.Verify(x => x.AddAsync(It.IsAny<SolicitudFactura>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateSolicitudFacturaCommandHandler_WhenClientNotFound_ShouldThrowInvalidOperationException()
    {
        // Arrange
        _mockClienteRepo.Setup(x => x.GetByIdAsync(999))
            .ReturnsAsync((Cliente?)null);

        var command = new CreateSolicitudFacturaCommand
        {
            ClienteID = 999,
            Folio = "F-001",
            Total = 100m,
            UsoCFDI = "G01"
        };
        var handler = new CreateSolicitudFacturaCommandHandler(_mockUnitOfWork.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cliente con ID 999 no encontrado*");

        _mockSolicitudRepo.Verify(x => x.AddAsync(It.IsAny<SolicitudFactura>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateSolicitudFacturaCommandHandler_WhenClientHasNoBillingData_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var clienteSinDatosFacturacion = Cliente.Create("5551234567", "Cliente Sin Factura");
        _mockClienteRepo.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(clienteSinDatosFacturacion);

        var command = new CreateSolicitudFacturaCommand
        {
            ClienteID = 2,
            Folio = "F-002",
            Total = 200m,
            UsoCFDI = "G01"
        };
        var handler = new CreateSolicitudFacturaCommandHandler(_mockUnitOfWork.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*El cliente no tiene datos de facturación registrados*");

        _mockSolicitudRepo.Verify(x => x.AddAsync(It.IsAny<SolicitudFactura>()), Times.Never);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetAllSolicitudesFacturaQueryHandler Tests

    [Fact]
    public async Task GetAllSolicitudesFacturaQueryHandler_ShouldReturnMappedDtosOrderedByFechaDesc()
    {
        // Arrange
        var (_, sol1) = CreateTestEntities(1, 101, usoCfdi: "G01", regimen: "601");
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.FechaSolicitud))?
            .SetValue(sol1, DateTime.UtcNow.AddHours(-2));

        var (_, sol2) = CreateTestEntities(2, 102, usoCfdi: "G03", regimen: "612");
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.FechaSolicitud))?
            .SetValue(sol2, DateTime.UtcNow.AddMinutes(-10));

        _mockSolicitudRepo.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<SolicitudFactura> { sol1, sol2 });

        var handler = new GetAllSolicitudesFacturaQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(new GetAllSolicitudesFacturaQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().SolicitudFacturaID.Should().Be(102); // Newer first
        result.Last().SolicitudFacturaID.Should().Be(101);

        // Verify catalogs resolved
        result.First().UsoCFDIDescripcion.Should().NotBeNullOrEmpty();
        result.First().RegimenFiscalDescripcion.Should().NotBeNullOrEmpty();
    }

    #endregion

    #region GetSolicitudFacturaByIdQueryHandler Tests

    [Fact]
    public async Task GetSolicitudFacturaByIdQueryHandler_WhenExists_ShouldReturnDto()
    {
        // Arrange
        var (_, solicitud) = CreateTestEntities(1, 100);
        _mockSolicitudRepo.Setup(x => x.GetByIdAsync(100L))
            .ReturnsAsync(solicitud);

        var handler = new GetSolicitudFacturaByIdQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(new GetSolicitudFacturaByIdQuery { SolicitudFacturaID = 100L }, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.SolicitudFacturaID.Should().Be(100);
        result.ClienteNombre.Should().Be("Empresa Test");
        result.Total.Should().Be(1500.50m);
    }

    [Fact]
    public async Task GetSolicitudFacturaByIdQueryHandler_WhenNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockSolicitudRepo.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((SolicitudFactura?)null);

        var handler = new GetSolicitudFacturaByIdQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(new GetSolicitudFacturaByIdQuery { SolicitudFacturaID = 999L }, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetSolicitudesFacturaByClienteQueryHandler Tests

    [Fact]
    public async Task GetSolicitudesFacturaByClienteQueryHandler_ShouldReturnClientSolicitudes()
    {
        // Arrange
        var (_, solicitud) = CreateTestEntities(5, 200);
        _mockSolicitudRepo.Setup(x => x.GetByClienteIdAsync(5))
            .ReturnsAsync(new List<SolicitudFactura> { solicitud });

        var handler = new GetSolicitudesFacturaByClienteQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(new GetSolicitudesFacturaByClienteQuery { ClienteID = 5 }, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().ClienteID.Should().Be(5);
        result.First().SolicitudFacturaID.Should().Be(200);
    }

    #endregion

    #region UpdateSolicitudFacturaEstadoCommandHandler Tests

    [Fact]
    public async Task UpdateSolicitudFacturaEstadoCommandHandler_WhenExists_ShouldUpdateStateAndNotes()
    {
        // Arrange
        var (_, solicitud) = CreateTestEntities(1, 100);
        _mockSolicitudRepo.Setup(x => x.GetByIdAsync(100L))
            .ReturnsAsync(solicitud);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateSolicitudFacturaEstadoCommand
        {
            SolicitudFacturaID = 100L,
            NuevoEstado = "Completada",
            Notas = "Factura emitida con folio SAT XYZ"
        };
        var handler = new UpdateSolicitudFacturaEstadoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        solicitud.Estado.Should().Be(EstadoSolicitudFactura.Completada);
        solicitud.Notas.Should().Be("Factura emitida con folio SAT XYZ");
        solicitud.FechaProcesada.Should().NotBeNull();

        _mockSolicitudRepo.Verify(x => x.UpdateAsync(solicitud), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSolicitudFacturaEstadoCommandHandler_WhenNotFound_ShouldReturnFalse()
    {
        // Arrange
        _mockSolicitudRepo.Setup(x => x.GetByIdAsync(999L))
            .ReturnsAsync((SolicitudFactura?)null);

        var command = new UpdateSolicitudFacturaEstadoCommand
        {
            SolicitudFacturaID = 999L,
            NuevoEstado = "Completada"
        };
        var handler = new UpdateSolicitudFacturaEstadoCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockSolicitudRepo.Verify(x => x.UpdateAsync(It.IsAny<SolicitudFactura>()), Times.Never);
    }

    [Fact]
    public async Task UpdateSolicitudFacturaEstadoCommandHandler_WhenInvalidState_ShouldThrowArgumentException()
    {
        // Arrange
        var (_, solicitud) = CreateTestEntities(1, 100);
        _mockSolicitudRepo.Setup(x => x.GetByIdAsync(100L))
            .ReturnsAsync(solicitud);

        var command = new UpdateSolicitudFacturaEstadoCommand
        {
            SolicitudFacturaID = 100L,
            NuevoEstado = "EstadoInvalidoQueNoExiste"
        };
        var handler = new UpdateSolicitudFacturaEstadoCommandHandler(_mockUnitOfWork.Object);

        // Act & Assert
        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Estado inválido*");
    }

    #endregion
}
