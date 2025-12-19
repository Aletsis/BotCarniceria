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

public class SessionHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ISessionRepository> _mockSessionRepository;
    private readonly Mock<IMessageRepository> _mockMessageRepository;
    private readonly Mock<IClienteRepository> _mockClientRepository;
    private readonly Mock<IConfiguracionRepository> _mockSettings;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;

    public SessionHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockSessionRepository = new Mock<ISessionRepository>();
        _mockMessageRepository = new Mock<IMessageRepository>();
        _mockClientRepository = new Mock<IClienteRepository>();
        _mockSettings = new Mock<IConfiguracionRepository>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        
        _mockUnitOfWork.Setup(x => x.Sessions).Returns(_mockSessionRepository.Object);
        _mockUnitOfWork.Setup(x => x.Messages).Returns(_mockMessageRepository.Object);
        _mockUnitOfWork.Setup(x => x.Clientes).Returns(_mockClientRepository.Object);
        _mockUnitOfWork.Setup(x => x.Settings).Returns(_mockSettings.Object);
    }

    #region GetActiveChatsQueryHandler Tests

    [Fact]
    public async Task GetActiveChatsQueryHandler_ShouldReturnChatsOrderedByLastActivity()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var olderSession = Conversacion.Create("5551111111");
        // Use reflection or internal method if UltimaActividad setter is private, 
        // but Create sets it to Now. We can assume we need to modify it or mock it if possible.
        // Since UltimaActividad is set on creation/update, let's just create them and assume the test runs fast enough?
        // No, we should ensure different processing times.
        // Actually, Conversacion entity might have private set.
        // Let's rely on creation time if we can't set it easily, 
        // OR better: use reflection to force values for testing ordering deterministically.
        
        typeof(Conversacion).GetProperty(nameof(Conversacion.UltimaActividad))?
            .SetValue(olderSession, now.AddMinutes(-10));

        var newerSession = Conversacion.Create("5552222222");
        typeof(Conversacion).GetProperty(nameof(Conversacion.UltimaActividad))?
            .SetValue(newerSession, now.AddMinutes(-1));;

        var sessions = new List<Conversacion> { olderSession, newerSession };

        _mockSessionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(sessions);

        // Mock Clientes to return null (no client found)
        _mockClientRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ReturnsAsync((Cliente?)null);

        var query = new GetActiveChatsQuery();
        var handler = new GetActiveChatsQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.First().NumeroTelefono.Should().Be("5552222222"); // Newer first
        result.Last().NumeroTelefono.Should().Be("5551111111");  // Older last
    }

    [Fact]
    public async Task GetActiveChatsQueryHandler_WhenClientExists_ShouldReturnClientName()
    {
        // Arrange
        var phone = "5551234567";
        var session = Conversacion.Create(phone);
        session.GuardarNombreTemporal("Temp Name"); // Should be ignored if client exists

        _mockSessionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Conversacion> { session });

        var client = Cliente.Create(phone, "Real Client Name");
        _mockClientRepository.Setup(x => x.GetByPhoneAsync(phone))
            .ReturnsAsync(client);

        var query = new GetActiveChatsQuery();
        var handler = new GetActiveChatsQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Real Client Name");
    }

    [Fact]
    public async Task GetActiveChatsQueryHandler_WhenNoClientButTempName_ShouldReturnTempName()
    {
        // Arrange
        var phone = "5551234567";
        var session = Conversacion.Create(phone);
        session.GuardarNombreTemporal("Temp Name");

        _mockSessionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Conversacion> { session });

        _mockClientRepository.Setup(x => x.GetByPhoneAsync(phone))
            .ReturnsAsync((Cliente?)null);

        var query = new GetActiveChatsQuery();
        var handler = new GetActiveChatsQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Temp Name");
    }

    [Fact]
    public async Task GetActiveChatsQueryHandler_WhenNoClientAndNoTempName_ShouldReturnDesconocido()
    {
        // Arrange
        var phone = "5551234567";
        var session = Conversacion.Create(phone);
        // session.NombreTemporal is null/empty by default or if not set

        _mockSessionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Conversacion> { session });

        _mockClientRepository.Setup(x => x.GetByPhoneAsync(phone))
            .ReturnsAsync((Cliente?)null);

        var query = new GetActiveChatsQuery();
        var handler = new GetActiveChatsQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        result.First().Nombre.Should().Be("Desconocido");
    }

    #endregion

    #region ResetSessionCommand Tests

    [Fact]
    public async Task ResetSessionCommand_WhenSessionExists_ShouldResetState()
    {
        // Arrange
        var existingSession = Conversacion.Create("5551234567");
        existingSession.CambiarEstado(ConversationState.TAKING_ORDER);
        existingSession.GuardarBuffer("some data");
        
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(existingSession);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new ResetSessionCommand { PhoneNumber = "5551234567" };
        var handler = new ResetSessionCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        existingSession.Estado.Should().Be(ConversationState.START);
        existingSession.Buffer.Should().BeNullOrEmpty();
        _mockSessionRepository.Verify(x => x.UpdateAsync(It.IsAny<Conversacion>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetSessionCommand_WhenSessionDoesNotExist_ShouldReturnFalse()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ReturnsAsync((Conversacion?)null);

        var command = new ResetSessionCommand { PhoneNumber = "5551234567" };
        var handler = new ResetSessionCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockSessionRepository.Verify(x => x.UpdateAsync(It.IsAny<Conversacion>()), Times.Never);
    }

    #endregion

    #region UpdateSessionStateCommand Tests

    [Fact]
    public async Task UpdateSessionStateCommand_ShouldUpdateStateSuccessfully()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(session);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateSessionStateCommand
        {
            PhoneNumber = "5551234567",
            NewState = "TAKING_ORDER"
        };
        var handler = new UpdateSessionStateCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        session.Estado.Should().Be(ConversationState.TAKING_ORDER);
        _mockSessionRepository.Verify(x => x.UpdateAsync(It.IsAny<Conversacion>()), Times.Once);
    }

    [Fact]
    public async Task UpdateSessionStateCommand_WithBuffer_ShouldSaveBuffer()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(session);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateSessionStateCommand
        {
            PhoneNumber = "5551234567",
            NewState = "TAKING_ORDER",
            Buffer = "2 kg de carne molida"
        };
        var handler = new UpdateSessionStateCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        session.Buffer.Should().Be("2 kg de carne molida");
    }

    [Fact]
    public async Task UpdateSessionStateCommand_WithNombreTemporal_ShouldSaveName()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(session);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateSessionStateCommand
        {
            PhoneNumber = "5551234567",
            NewState = "ASK_ADDRESS",
            NombreTemporal = "Juan Pérez"
        };
        var handler = new UpdateSessionStateCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        session.NombreTemporal.Should().Be("Juan Pérez");
    }

    [Fact]
    public async Task UpdateSessionStateCommand_WhenSessionNotFound_ShouldCreateNew()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ReturnsAsync((Conversacion?)null);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new UpdateSessionStateCommand
        {
            PhoneNumber = "5559999999",
            NewState = "MENU"
        };
        var handler = new UpdateSessionStateCommandHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockSessionRepository.Verify(x => x.AddAsync(It.IsAny<Conversacion>()), Times.Once);
    }

    #endregion

    #region GetSessionByPhoneQuery Tests

    [Fact]
    public async Task GetSessionByPhoneQuery_WhenSessionExists_ShouldReturnDto()
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        session.CambiarEstado(ConversationState.MENU);
        
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(session);

        var query = new GetSessionByPhoneQuery { PhoneNumber = "5551234567" };
        var handler = new GetSessionByPhoneQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.NumeroTelefono.Should().Be("5551234567");
        result.Estado.Should().Be(ConversationState.MENU.ToString());
    }

    [Fact]
    public async Task GetSessionByPhoneQuery_WhenSessionNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ReturnsAsync((Conversacion?)null);

        var query = new GetSessionByPhoneQuery { PhoneNumber = "5559999999" };
        var handler = new GetSessionByPhoneQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsSessionExpiredQuery Tests

    [Fact]
    public async Task IsSessionExpiredQuery_WhenSessionExpired_ShouldReturnTrue()
    {
        // Arrange
        var session = Conversacion.Create("5551234567", 0); // 0 minutes timeout
        System.Threading.Thread.Sleep(100); // Wait for expiration
        
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(session);

        var query = new IsSessionExpiredQuery { PhoneNumber = "5551234567" };
        var handler = new IsSessionExpiredQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSessionExpiredQuery_WhenSessionNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var session = Conversacion.Create("5551234567", 30);
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync("5551234567"))
            .ReturnsAsync(session);

        var query = new IsSessionExpiredQuery { PhoneNumber = "5551234567" };
        var handler = new IsSessionExpiredQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSessionExpiredQuery_WhenSessionNotFound_ShouldReturnTrue()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.GetByPhoneAsync(It.IsAny<string>()))
            .ReturnsAsync((Conversacion?)null);

        var query = new IsSessionExpiredQuery { PhoneNumber = "5559999999" };
        var handler = new IsSessionExpiredQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetAllConversationsQuery Tests

    [Fact]
    public async Task GetAllConversationsQuery_ShouldReturnAllConversations()
    {
        // Arrange
        var conversations = new List<Conversacion>
        {
            Conversacion.Create("5551111111"),
            Conversacion.Create("5552222222"),
            Conversacion.Create("5553333333")
        };

        _mockSessionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(conversations);

        var query = new GetAllConversationsQuery();
        var handler = new GetAllConversationsQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(c => c.NumeroTelefono == "5551111111");
        result.Should().Contain(c => c.NumeroTelefono == "5552222222");
        result.Should().Contain(c => c.NumeroTelefono == "5553333333");
    }

    [Fact]
    public async Task GetAllConversationsQuery_WhenNoConversations_ShouldReturnEmptyList()
    {
        // Arrange
        _mockSessionRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Conversacion>());

        var query = new GetAllConversationsQuery();
        var handler = new GetAllConversationsQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region SendWhatsAppMessageCommand Tests

    [Fact]
    public async Task SendWhatsAppMessageCommand_ShouldSendMessageSuccessfully()
    {
        // Arrange
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new SendWhatsAppMessageCommand
        {
            PhoneNumber = "5551234567",
            Message = "Test message"
        };
        var handler = new SendWhatsAppMessageCommandHandler(_mockWhatsAppService.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeTrue();
        _mockWhatsAppService.Verify(x => x.SendTextMessageAsync("5551234567", "Test message"), Times.Once);
        // The Service is responsible for persistence in the current implementation, not the Handler directly.
        // so we do not verify _mockMessageRepository.AddAsync here.
    }

    [Fact]
    public async Task SendWhatsAppMessageCommand_WhenSendFails_ShouldReturnFalse()
    {
        // Arrange
        _mockWhatsAppService.Setup(x => x.SendTextMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Send failed"));

        var command = new SendWhatsAppMessageCommand
        {
            PhoneNumber = "5551234567",
            Message = "Test message"
        };
        var handler = new SendWhatsAppMessageCommandHandler(_mockWhatsAppService.Object, _mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeFalse();
        _mockMessageRepository.Verify(x => x.AddAsync(It.IsAny<Mensaje>()), Times.Never);
    }

    #endregion
}
