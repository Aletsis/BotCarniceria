using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class BillingStateHandlerTests
{
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<IClienteRepository> _mockClienteRepo;
    private readonly Mock<IUsuarioRepository> _mockUserRepo;
    private readonly BillingStateHandler _handler;

    private const string TestPhone = "5551234567";

    public BillingStateHandlerTests()
    {
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMediator = new Mock<IMediator>();
        _mockClienteRepo = new Mock<IClienteRepository>();
        _mockUserRepo = new Mock<IUsuarioRepository>();

        _mockUnitOfWork.Setup(u => u.Clientes).Returns(_mockClienteRepo.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepo.Object);

        _handler = new BillingStateHandler(
            _mockWhatsAppService.Object,
            _mockUnitOfWork.Object,
            _mockMediator.Object);
    }

    [Fact]
    public async Task HandleAsync_WhenClienteNotFound_ShouldSendErrorMessageAndSetMenu()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_WARNING);

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(TestPhone, "billing_warning_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, "Error: Cliente no encontrado."), Times.Once);
        session.Estado.Should().Be(ConversationState.MENU);
    }

    #region BILLING_WARNING Tests

    [Fact]
    public async Task HandleAsync_BillingWarning_Continue_WithExistingBillingData_ShouldShowConfirmation()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_WARNING);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez", "Calle 1");
        cliente.UpdateDatosFacturacion(new DatosFacturacion(
            "Empresa SA", "XAXX010101000", "Calle 1", "100", "Centro", "12345", "test@test.com", "601"));

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "billing_warning_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendInteractiveButtonsAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("Confirma tus Datos") && msg.Contains("Empresa SA")),
            It.Is<List<(string id, string title)>>(b => b.Any(x => x.id == "billing_confirm") && b.Any(x => x.id == "billing_correct")),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_CONFIRM_DATA);
    }

    [Fact]
    public async Task HandleAsync_BillingWarning_Continue_WithoutBillingData_ShouldAskRazonSocial()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_WARNING);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez", "Calle 1");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "billing_warning_continue", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("Razón Social"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_ASK_RAZON_SOCIAL);
    }

    [Fact]
    public async Task HandleAsync_BillingWarning_Cancel_ShouldSendCancelledAndSetMenu()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_WARNING);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "billing_warning_cancel", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("cancelada"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.MENU);
    }

    [Fact]
    public async Task HandleAsync_BillingWarning_InvalidInput_ShouldSendSelectValidOption()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_WARNING);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "invalid_choice", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            "Por favor, selecciona una opción válida."),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_WARNING);
    }

    #endregion

    #region Input Type Validations

    [Theory]
    [InlineData(ConversationState.BILLING_ASK_RAZON_SOCIAL, "Razón Social")]
    [InlineData(ConversationState.BILLING_ASK_RFC, "RFC")]
    [InlineData(ConversationState.BILLING_ASK_CALLE, "calle")]
    [InlineData(ConversationState.BILLING_ASK_NUMERO, "número")]
    [InlineData(ConversationState.BILLING_ASK_COLONIA, "colonia")]
    [InlineData(ConversationState.BILLING_ASK_CP, "código postal")]
    [InlineData(ConversationState.BILLING_ASK_CORREO, "correo")]
    [InlineData(ConversationState.BILLING_ASK_NOTE_FOLIO, "folio")]
    [InlineData(ConversationState.BILLING_ASK_NOTE_TOTAL, "total")]
    public async Task HandleAsync_NonTextMessage_ShouldSendInvalidResponseMessageAndNotAdvance(ConversationState state, string expectedKeyword)
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(state);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "image_url.jpg", TipoContenidoMensaje.Imagen, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("Respuesta no válida") && msg.Contains(expectedKeyword))),
            Times.Once);

        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        session.Estado.Should().Be(state);
    }

    #endregion

    #region Step-by-Step Data Capture Tests

    [Fact]
    public async Task HandleAsync_BillingAskRazonSocial_ValidInput_ShouldUpdateAndAskRfc()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_RAZON_SOCIAL);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "Carnes Selectas SA de CV", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion.Should().NotBeNull();
        cliente.DatosFacturacion!.RazonSocial.Should().Be("Carnes Selectas SA de CV");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("RFC"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_RFC);
    }

    [Fact]
    public async Task HandleAsync_BillingAskRfc_ValidInput_ShouldUpdateAndAskCalle()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_RFC);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "XAXX010101000", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion!.RFC.Should().Be("XAXX010101000");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("Calle"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_CALLE);
    }

    [Fact]
    public async Task HandleAsync_BillingAskCalle_ValidInput_ShouldUpdateAndAskNumero()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_CALLE);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "Av. Revolución", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion!.Calle.Should().Be("Av. Revolución");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("Número"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_NUMERO);
    }

    [Fact]
    public async Task HandleAsync_BillingAskNumero_ValidInput_ShouldUpdateAndAskColonia()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_NUMERO);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "402 Int. 5", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion!.Numero.Should().Be("402 Int. 5");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("Colonia"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_COLONIA);
    }

    [Fact]
    public async Task HandleAsync_BillingAskColonia_ValidInput_ShouldUpdateAndAskCp()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_COLONIA);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "Centro", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion!.Colonia.Should().Be("Centro");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("Código Postal"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_CP);
    }

    [Fact]
    public async Task HandleAsync_BillingAskCp_ValidInput_ShouldUpdateAndAskCorreo()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_CP);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "64000", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion!.CodigoPostal.Should().Be("64000");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("Correo"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_CORREO);
    }

    [Fact]
    public async Task HandleAsync_BillingAskCorreo_ValidInput_ShouldUpdateAndSendInteractiveRegimenList()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_CORREO);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "facturacion@test.com", TipoContenidoMensaje.Texto, session);

        // Assert
        cliente.DatosFacturacion!.Correo.Should().Be("facturacion@test.com");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendInteractiveListAsync(
            TestPhone,
            It.Is<string>(m => m.Contains("Régimen Fiscal")),
            "Ver Regímenes",
            It.Is<List<(string id, string title, string? description)>>(l => l.Any(r => r.id == "601")),
            "Selecciona tu régimen fiscal:",
            "Opciones Disponibles"),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_ASK_REGIMEN);
    }

    [Fact]
    public async Task HandleAsync_BillingAskRegimen_ValidInput_ShouldUpdateAndShowConfirmation()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_REGIMEN);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        cliente.UpdateDatosFacturacion(new DatosFacturacion(
            "Mi Empresa", "XAXX010101000", "Calle 1", "10", "Colonia 1", "64000", "test@test.com", ""));

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "601", TipoContenidoMensaje.Interactivo, session);

        // Assert
        cliente.DatosFacturacion!.RegimenFiscal.Should().Be("601");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendInteractiveButtonsAsync(
            TestPhone,
            It.Is<string>(m => m.Contains("Confirma tus Datos") && m.Contains("601")),
            It.IsAny<List<(string id, string title)>>(),
            It.IsAny<string?>(),
            It.IsAny<string?>()),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_CONFIRM_DATA);
    }

    #endregion

    #region BILLING_CONFIRM_DATA Tests

    [Fact]
    public async Task HandleAsync_BillingConfirmData_Confirm_ShouldAskNoteFolio()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_CONFIRM_DATA);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "billing_confirm", TipoContenidoMensaje.Interactivo, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(m => m.Contains("Folio de la Nota"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_ASK_NOTE_FOLIO);
    }

    [Fact]
    public async Task HandleAsync_BillingConfirmData_Correct_ShouldRestartAtRazonSocial()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_CONFIRM_DATA);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "billing_correct", TipoContenidoMensaje.Interactivo, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(m => m.Contains("Vamos a corregir") && m.Contains("Razón Social"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_ASK_RAZON_SOCIAL);
    }

    [Fact]
    public async Task HandleAsync_BillingConfirmData_InvalidOption_ShouldShowWarningAndResendConfirmation()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_CONFIRM_DATA);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        cliente.UpdateDatosFacturacion(new DatosFacturacion(
            "Mi Empresa", "XAXX010101000", "Calle 1", "10", "Colonia 1", "64000", "test@test.com", "601"));

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "otra_cosa", TipoContenidoMensaje.Texto, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, "Por favor, selecciona una opción válida."), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendInteractiveButtonsAsync(TestPhone, It.IsAny<string>(), It.IsAny<List<(string id, string title)>>(), It.IsAny<string?>(), It.IsAny<string?>()), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_CONFIRM_DATA);
    }

    #endregion

    #region Ticket Details & Submission Tests

    [Fact]
    public async Task HandleAsync_BillingAskNoteFolio_ValidInput_ShouldSetFolioAndAskTotal()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_NOTE_FOLIO);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "TICKET-12345", TipoContenidoMensaje.Texto, session);

        // Assert
        session.FacturaTemp_Folio.Should().Be("TICKET-12345");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(TestPhone, It.Is<string>(m => m.Contains("Total de la Nota"))), Times.Once);
        session.Estado.Should().Be(ConversationState.BILLING_ASK_NOTE_TOTAL);
    }

    [Fact]
    public async Task HandleAsync_BillingAskNoteTotal_ValidInput_ShouldSetTotalAndSendCfdiList()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_NOTE_TOTAL);

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "450.50", TipoContenidoMensaje.Texto, session);

        // Assert
        session.FacturaTemp_Total.Should().Be("450.50");
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendInteractiveListAsync(
            TestPhone,
            It.Is<string>(m => m.Contains("Uso de CFDI")),
            "Ver Usos",
            It.Is<List<(string id, string title, string? description)>>(l => l.Any(u => u.id == "G01")),
            "Selecciona el uso CFDI:",
            "Usos Disponibles"),
            Times.Once);

        session.Estado.Should().Be(ConversationState.BILLING_ASK_CFDI);
    }

    [Fact]
    public async Task HandleAsync_BillingAskCfdi_WithInvalidDecimalTotal_ShouldSendErrorMessageAndSetMenu()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_CFDI);
        session.SetFacturaTemp_Folio("FOLIO-01");
        session.SetFacturaTemp_Total("invalid_amount");

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);

        // Act
        await _handler.HandleAsync(TestPhone, "G01", TipoContenidoMensaje.Interactivo, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(m => m.Contains("El total ingresado no es válido"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.MENU);
        _mockMediator.Verify(m => m.Send(It.IsAny<CreateSolicitudFacturaCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_BillingAskCfdi_WithValidData_ShouldSendMediatorCommandNotifySupervisorsAndSetMenu()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_CFDI);
        session.SetFacturaTemp_Folio("TICKET-999");
        session.SetFacturaTemp_Total("850.00");

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        cliente.UpdateDatosFacturacion(new DatosFacturacion(
            "Empresa Test", "XAXX010101000", "Calle 1", "10", "Centro", "64000", "test@test.com", "601"));

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);
        _mockMediator.Setup(m => m.Send(It.IsAny<CreateSolicitudFacturaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(777);

        var supervisor1 = Usuario.Create("sup1", "hash", "Supervisor 1", RolUsuario.Supervisor, "5558881111");
        var supervisor2 = Usuario.Create("sup2", "hash", "Supervisor 2", RolUsuario.Supervisor, "5558882222");
        var supervisorNoPhone = Usuario.Create("sup3", "hash", "Supervisor 3", RolUsuario.Supervisor, null);

        _mockUserRepo.Setup(u => u.FindAsync(It.IsAny<SupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario> { supervisor1, supervisor2, supervisorNoPhone });

        // Act
        await _handler.HandleAsync(TestPhone, "G01", TipoContenidoMensaje.Interactivo, session);

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<CreateSolicitudFacturaCommand>(cmd =>
            cmd.ClienteID == cliente.ClienteID &&
            cmd.Folio == "TICKET-999" &&
            cmd.Total == 850.00m &&
            cmd.UsoCFDI == "G01"), It.IsAny<CancellationToken>()), Times.Once);

        // Verify supervisor notifications
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            "5558881111",
            It.Is<string>(msg => msg.Contains("Nueva Solicitud de Factura") && msg.Contains("TICKET-999"))),
            Times.Once);

        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            "5558882222",
            It.Is<string>(msg => msg.Contains("Nueva Solicitud de Factura") && msg.Contains("TICKET-999"))),
            Times.Once);

        // Verify client receipt confirmation with generated ID #777
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("Solicitud Recibida") && msg.Contains("#777"))),
            Times.Once);

        // Verify final greeting to return to menu
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("Hola"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.MENU);
    }

    [Fact]
    public async Task HandleAsync_BillingAskCfdi_WhenMediatorFails_ShouldSendErrorMessageAndSetMenu()
    {
        // Arrange
        var session = Conversacion.Create(TestPhone);
        session.CambiarEstado(ConversationState.BILLING_ASK_CFDI);
        session.SetFacturaTemp_Folio("TICKET-999");
        session.SetFacturaTemp_Total("850.00");

        var cliente = Cliente.Create(TestPhone, "Juan Pérez");
        cliente.UpdateDatosFacturacion(new DatosFacturacion(
            "Empresa Test", "XAXX010101000", "Calle 1", "10", "Centro", "64000", "test@test.com", "601"));

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(TestPhone)).ReturnsAsync(cliente);
        _mockMediator.Setup(m => m.Send(It.IsAny<CreateSolicitudFacturaCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB connection failure"));

        // Act
        await _handler.HandleAsync(TestPhone, "G01", TipoContenidoMensaje.Interactivo, session);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            TestPhone,
            It.Is<string>(msg => msg.Contains("Error al procesar tu solicitud") && msg.Contains("DB connection failure"))),
            Times.Once);

        session.Estado.Should().Be(ConversationState.MENU);
    }

    #endregion
}
