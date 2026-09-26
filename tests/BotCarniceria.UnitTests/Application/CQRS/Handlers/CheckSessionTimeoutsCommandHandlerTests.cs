using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class CheckSessionTimeoutsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IUsuarioRepository> _mockUsuarioRepository;
    private readonly Mock<IConfiguracionRepository> _mockSettings;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<ILogger<CheckSessionTimeoutsCommandHandler>> _mockLogger;
    private readonly CheckSessionTimeoutsCommandHandler _handler;

    public CheckSessionTimeoutsCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockUsuarioRepository = new Mock<IUsuarioRepository>();
        _mockSettings = new Mock<IConfiguracionRepository>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockLogger = new Mock<ILogger<CheckSessionTimeoutsCommandHandler>>();

        _mockUnitOfWork.Setup(x => x.Sessions).Returns(_mockSessionRepository.Object);
        _mockUnitOfWork.Setup(x => x.Users).Returns(_mockUsuarioRepository.Object);
        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettings.Object);

        _handler = new CheckSessionTimeoutsCommandHandler(
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WithConfiguredSettings_ProcessesAllThreeStages()
    {
        // Arrange
        _mockSettings.Setup(x => x.GetValorAsync(ConfigurationKeys.Session.BotTimeoutMinutes))
            .ReturnsAsync("45");
        _mockSettings.Setup(x => x.GetValorAsync(ConfigurationKeys.Session.BotWarningMinutes))
            .ReturnsAsync("5");

        var expiringSession = Conversacion.Create("5551111111", 45);
        expiringSession.CambiarEstado(ConversationState.TAKING_ORDER);

        var adminUser = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin, "5552222222");
        var expiring24hSession = Conversacion.Create("5552222222", 45);

        var expiredSession = Conversacion.Create("5553333333", 45);
        expiredSession.CambiarEstado(ConversationState.TAKING_ORDER);
        expiredSession.GuardarBuffer("algun buffer");

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { expiringSession });

        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario> { adminUser });

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<Expiring24hSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { expiring24hSession });

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { expiredSession });

        // Act
        var result = await _handler.Handle(new CheckSessionTimeoutsCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);

        // 1. Expiring session notified
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            "5551111111",
            It.Is<string>(msg => msg.Contains("Aviso de inactividad"))), Times.Once);
        expiringSession.NotificacionTimeoutEnviada.Should().BeTrue();

        // 2. Expiring 24h session notified
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            "5552222222",
            It.Is<string>(msg => msg.Contains("Aviso de inactividad prolongada"))), Times.Once);
        expiring24hSession.Notificacion24hEnviada.Should().BeTrue();

        // 3. Expired session reset and notified
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            "5553333333",
            It.Is<string>(msg => msg.Contains("Sesión expirada"))), Times.Once);
        expiredSession.Estado.Should().Be(ConversationState.START);
        expiredSession.Buffer.Should().BeNull();

        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task Handle_WithDefaultSettingsWhenNull_UsesDefaults()
    {
        // Arrange
        _mockSettings.Setup(x => x.GetValorAsync(It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());
        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario>());
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());

        // Act
        var result = await _handler.Handle(new CheckSessionTimeoutsCommand(), CancellationToken.None);

        // Assert
        result.Should().Be(MediatR.Unit.Value);
        _mockSessionRepository.Verify(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()), Times.Once);
        _mockSessionRepository.Verify(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()), Times.Once);
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessExpiringSessions_WhenNoSessionsFound_DoesNotSendMessages()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());
        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario>());
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());

        // Act
        await _handler.Handle(new CheckSessionTimeoutsCommand(), CancellationToken.None);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            It.IsAny<string>(),
            It.Is<string>(m => m.Contains("Aviso de inactividad"))), Times.Never);
    }

    [Fact]
    public async Task ProcessExpiring24hSessions_WhenNoAdminsWithPhone_DoesNotQuery24hSessions()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());
        
        // No users with phone
        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario>());

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());

        // Act
        await _handler.Handle(new CheckSessionTimeoutsCommand(), CancellationToken.None);

        // Assert
        _mockSessionRepository.Verify(x => x.FindAsync(It.IsAny<Expiring24hSessionsSpecification>()), Times.Never);
    }

    [Fact]
    public async Task ProcessExpiring24hSessions_WhenSessionsDoNotMatchTargetPhones_DoesNotSendMessages()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());

        var admin = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin, "5551111111");
        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario> { admin });

        // Session phone does not match admin phone
        var nonAdminSession = Conversacion.Create("5559999999");
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<Expiring24hSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { nonAdminSession });

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());

        // Act
        await _handler.Handle(new CheckSessionTimeoutsCommand(), CancellationToken.None);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            "5559999999",
            It.Is<string>(m => m.Contains("prolongada"))), Times.Never);
        nonAdminSession.Notificacion24hEnviada.Should().BeFalse();
    }

    [Fact]
    public async Task ProcessExpiredSessions_WhenSessionsFound_ResetsToStartStateAndCleansBuffer()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion>());
        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario>());

        var session1 = Conversacion.Create("5551234567");
        session1.CambiarEstado(ConversationState.CONFIRM_ADDRESS);
        session1.GuardarBuffer("Calle Falsa 123");

        var session2 = Conversacion.Create("5557654321");
        session2.CambiarEstado(ConversationState.MENU);

        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { session1, session2 });

        // Act
        await _handler.Handle(new CheckSessionTimeoutsCommand(), CancellationToken.None);

        // Assert
        session1.Estado.Should().Be(ConversationState.START);
        session1.Buffer.Should().BeNull();
        session2.Estado.Should().Be(ConversationState.START);
        session2.Buffer.Should().BeNull();

        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            "5551234567",
            It.Is<string>(m => m.Contains("Sesión expirada"))), Times.Once);
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(
            "5557654321",
            It.Is<string>(m => m.Contains("Sesión expirada"))), Times.Once);

        _mockSessionRepository.Verify(x => x.UpdateAsync(session1), Times.Once);
        _mockSessionRepository.Verify(x => x.UpdateAsync(session2), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenCancellationRequested_BreaksEarlyWithoutProcessingSubsequent()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled

        var session = Conversacion.Create("5551111111");
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiringSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { session });

        var admin = Usuario.Create("admin", "hash", "Admin", RolUsuario.Admin, "5552222222");
        _mockUsuarioRepository.Setup(x => x.FindAsync(It.IsAny<AdminsAndSupervisorsWithPhoneSpecification>()))
            .ReturnsAsync(new List<Usuario> { admin });

        var session24h = Conversacion.Create("5552222222");
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<Expiring24hSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { session24h });

        var expiredSession = Conversacion.Create("5553333333");
        _mockSessionRepository.Setup(x => x.FindAsync(It.IsAny<ExpiredSessionsSpecification>()))
            .ReturnsAsync(new List<Conversacion> { expiredSession });

        // Act
        await _handler.Handle(new CheckSessionTimeoutsCommand(), cts.Token);

        // Assert
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockSessionRepository.Verify(x => x.UpdateAsync(It.IsAny<Conversacion>()), Times.Never);
    }
}
