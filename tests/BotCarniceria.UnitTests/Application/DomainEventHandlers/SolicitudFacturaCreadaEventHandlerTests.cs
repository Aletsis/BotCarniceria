using BotCarniceria.Core.Application.DomainEventHandlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.Events;
using BotCarniceria.Core.Domain.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.DomainEventHandlers;

public class SolicitudFacturaCreadaEventHandlerTests
{
    private readonly Mock<IServiceScopeFactory> _mockScopeFactory;
    private readonly Mock<IServiceScope> _mockScope;
    private readonly Mock<IServiceProvider> _mockServiceProvider;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<ISolicitudFacturaRepository> _mockSolicitudesRepo;
    private readonly Mock<IClienteRepository> _mockClientesRepo;
    private readonly Mock<IUsuarioRepository> _mockUsuariosRepo;
    private readonly SolicitudFacturaCreadaEventHandler _handler;

    public SolicitudFacturaCreadaEventHandlerTests()
    {
        _mockScopeFactory = new Mock<IServiceScopeFactory>();
        _mockScope = new Mock<IServiceScope>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockSolicitudesRepo = new Mock<ISolicitudFacturaRepository>();
        _mockClientesRepo = new Mock<IClienteRepository>();
        _mockUsuariosRepo = new Mock<IUsuarioRepository>();

        _mockScopeFactory.Setup(x => x.CreateScope()).Returns(_mockScope.Object);
        _mockScope.Setup(x => x.ServiceProvider).Returns(_mockServiceProvider.Object);

        _mockServiceProvider.Setup(x => x.GetService(typeof(IUnitOfWork))).Returns(_mockUnitOfWork.Object);
        _mockServiceProvider.Setup(x => x.GetService(typeof(IWhatsAppService))).Returns(_mockWhatsAppService.Object);

        _mockUnitOfWork.Setup(x => x.SolicitudesFactura).Returns(_mockSolicitudesRepo.Object);
        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClientesRepo.Object);
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUsuariosRepo.Object);

        _handler = new SolicitudFacturaCreadaEventHandler(_mockScopeFactory.Object);
    }

    [Fact]
    public async Task Handle_WhenSolicitudAndClienteExist_SendsWhatsAppNotificationToSupervisors()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Carnes Premium");
        typeof(Cliente).GetProperty(nameof(Cliente.ClienteID))?.SetValue(cliente, 1);

        var datos = new DatosFacturacion(
            "Carnes Premium SA de CV", "CPR010101AA1", "Av Revolución", "500", "Moderna", "03500", "factura@carnes.com", "601");
        cliente.UpdateDatosFacturacion(datos);

        var solicitud = SolicitudFactura.Create(1, "FAC-901", 1250.00m, "G01", datos, "Entrega en sucursal");
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.SolicitudFacturaID))?.SetValue(solicitud, 50L);

        _mockSolicitudesRepo.Setup(x => x.GetByIdAsync(50L))
            .ReturnsAsync(solicitud);
        _mockClientesRepo.Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(cliente);

        var supervisor = Usuario.Create("supervisor1", "hash", "Super Visor", RolUsuario.Supervisor, "5559876543");
        _mockUsuariosRepo.Setup(x => x.FindAsync(It.IsAny<SupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario> { supervisor });

        var tcs = new TaskCompletionSource<string>();
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync("5559876543", It.IsAny<string>()))
            .ReturnsAsync(true)
            .Callback<string, string>((_, msg) => tcs.TrySetResult(msg));

        var domainEvent = new SolicitudFacturaCreadaDomainEvent(solicitud);

        // Act
        var resultTask = _handler.Handle(domainEvent, CancellationToken.None);

        // Assert Task completes immediately
        resultTask.IsCompletedSuccessfully.Should().BeTrue();

        // Wait for background worker
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        completed.Should().Be(tcs.Task, "el mensaje de WhatsApp en segundo plano debió enviarse dentro del tiempo límite");

        var message = await tcs.Task;
        message.Should().Contain("Nueva Solicitud de Factura");
        message.Should().Contain("Carnes Premium");
        message.Should().Contain("CPR010101AA1");
        message.Should().Contain("FAC-901");
        message.Should().Contain("1,250.00");
    }

    [Fact]
    public async Task Handle_WhenSupervisorHasNoPhone_DoesNotSendWhatsAppMessage()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Cliente Sin Tel");
        typeof(Cliente).GetProperty(nameof(Cliente.ClienteID))?.SetValue(cliente, 2);

        var datos = new DatosFacturacion(
            "Empresa SA", "XAXX010101000", "Calle 1", "1", "Col", "00000", "test@test.com", "601");
        cliente.UpdateDatosFacturacion(datos);

        var solicitud = SolicitudFactura.Create(2, "FAC-902", 500.00m, "G03", datos);
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.SolicitudFacturaID))?.SetValue(solicitud, 51L);

        _mockSolicitudesRepo.Setup(x => x.GetByIdAsync(51L))
            .ReturnsAsync(solicitud);
        _mockClientesRepo.Setup(x => x.GetByIdAsync(2))
            .ReturnsAsync(cliente);

        var supervisorSinTelefono = Usuario.Create("supervisor2", "hash", "Super Sin Tel", RolUsuario.Supervisor, null);
        
        var tcs = new TaskCompletionSource<bool>();
        _mockUsuariosRepo.Setup(x => x.FindAsync(It.IsAny<SupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario> { supervisorSinTelefono })
            .Callback(() => tcs.TrySetResult(true));

        var domainEvent = new SolicitudFacturaCreadaDomainEvent(solicitud);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Wait for query to execute
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        completed.Should().Be(tcs.Task);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDatosFacturacionNull_DoesNotSendMessage()
    {
        // Arrange
        var cliente = Cliente.Create("5551234567", "Cliente Test");
        typeof(Cliente).GetProperty(nameof(Cliente.ClienteID))?.SetValue(cliente, 3);

        var datos = new DatosFacturacion(
            "Empresa SA", "XAXX010101000", "Calle 1", "1", "Col", "00000", "test@test.com", "601");

        var solicitud = SolicitudFactura.Create(3, "FAC-903", 500.00m, "G03", datos);
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.SolicitudFacturaID))?.SetValue(solicitud, 52L);
        // Force DatosFacturacion to null via reflection
        typeof(SolicitudFactura).GetProperty(nameof(SolicitudFactura.DatosFacturacion))?.SetValue(solicitud, null);

        _mockSolicitudesRepo.Setup(x => x.GetByIdAsync(52L))
            .ReturnsAsync(solicitud);
        _mockClientesRepo.Setup(x => x.GetByIdAsync(3))
            .ReturnsAsync(cliente);

        var tcs = new TaskCompletionSource<bool>();
        _mockUsuariosRepo.Setup(x => x.FindAsync(It.IsAny<SupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario>())
            .Callback(() => tcs.TrySetResult(true));

        var domainEvent = new SolicitudFacturaCreadaDomainEvent(solicitud);

        // Act
        await _handler.Handle(domainEvent, CancellationToken.None);

        // Wait for query
        var completed = await Task.WhenAny(tcs.Task, Task.Delay(3000));
        completed.Should().Be(tcs.Task);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenScopeFactoryThrows_CatchesAndCompletesTask()
    {
        // Arrange
        _mockScopeFactory.Setup(x => x.CreateScope())
            .Throws(new InvalidOperationException("Scope failure"));

        var datos = new DatosFacturacion(
            "Empresa SA", "XAXX010101000", "Calle 1", "1", "Col", "00000", "test@test.com", "601");
        var solicitud = SolicitudFactura.Create(1, "FAC-904", 100m, "G01", datos);

        var domainEvent = new SolicitudFacturaCreadaDomainEvent(solicitud);

        // Act
        var act = async () => await _handler.Handle(domainEvent, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
