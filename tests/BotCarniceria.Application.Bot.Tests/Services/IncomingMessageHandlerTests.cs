using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.Services;
using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using FluentAssertions;

namespace BotCarniceria.Application.Bot.Tests.Services;

public class IncomingMessageHandlerTests
{
    private readonly Mock<IStateHandlerFactory> _mockFactory;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IRealTimeNotificationService> _mockNotificationService;
    private readonly Mock<ILogger<IncomingMessageHandler>> _mockLogger;
    private readonly Mock<IConversationStateHandler> _mockHandler;
    private readonly Mock<ISessionRepository> _mockSessionRepo;
    private readonly IncomingMessageHandler _service;

    public IncomingMessageHandlerTests()
    {
        _mockFactory = new Mock<IStateHandlerFactory>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockNotificationService = new Mock<IRealTimeNotificationService>();
        _mockLogger = new Mock<ILogger<IncomingMessageHandler>>();
        _mockHandler = new Mock<IConversationStateHandler>();
        _mockSessionRepo = new Mock<ISessionRepository>();
        var mockMessageRepo = new Mock<IMessageRepository>();

        _mockUnitOfWork.Setup(u => u.Sessions).Returns(_mockSessionRepo.Object);
        _mockUnitOfWork.Setup(u => u.Messages).Returns(mockMessageRepo.Object);
        _mockFactory.Setup(f => f.GetHandler(It.IsAny<ConversationState>())).Returns(_mockHandler.Object);

        _service = new IncomingMessageHandler(
            _mockFactory.Object,
            _mockUnitOfWork.Object,
            _mockWhatsAppService.Object,
            _mockNotificationService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task HandleAsync_NewSession_ShouldCreateSessionAndDelegateToHandler()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Id = "msg_123",
            Type = "text",
            Text = new WhatsAppText { Body = "Hola" }
        };

        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync((Conversacion?)null);

        // Act
        await _service.HandleAsync(message);

        // Assert
        // Verify Session Creation
        _mockSessionRepo.Verify(r => r.AddAsync(It.Is<Conversacion>(c => c.NumeroTelefono == message.From)), Times.Once);

        // Verify Mark as Read
        _mockWhatsAppService.Verify(w => w.MarkMessageAsReadAsync(message.Id), Times.Once);

        // Verify Factory called (Default state is START for new session but internally Logic might init it differently, 
        // Conversacion.Create sets START.
        _mockFactory.Verify(f => f.GetHandler(ConversationState.START), Times.Once);

        // Verify Handler execution
        _mockHandler.Verify(h => h.HandleAsync(message.From, "Hola", TipoContenidoMensaje.Texto, It.IsAny<Conversacion>()), Times.Once);

        // Verify Save
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Fact]
    public async Task HandleAsync_GlobalCommand_ShouldResetState()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "text",
            Text = new WhatsAppText { Body = "Menu" } // Case insensitive
        };

        var session = Conversacion.Create(message.From);
        session.CambiarEstado(ConversationState.TAKING_ORDER); // Set different state

        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);

        // Act
        await _service.HandleAsync(message);

        // Assert
        session.Estado.Should().Be(ConversationState.MENU);
        
        // Verify handler was called with new state
        _mockFactory.Verify(f => f.GetHandler(ConversationState.MENU), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InteractiveMessage_ShouldExtractId()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "interactive",
            Interactive = new WhatsAppInteractive
            {
                Button_Reply = new WhatsAppButtonReply { Id = "btn_start", Title = "Inicio" }
            }
        };

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockHandler.Verify(h => h.HandleAsync(message.From, "btn_start", TipoContenidoMensaje.Interactivo, session), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_Exception_ShouldNotifyUser()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "text",
            Text = new WhatsAppText { Body = "Crash" }
        };

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);
        
        _mockHandler.Setup(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TipoContenidoMensaje>(), It.IsAny<Conversacion>()))
            .ThrowsAsync(new Exception("Critical Error"));

        // Act
        await _service.HandleAsync(message);

        // Assert
        // Verify Error Log
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);

        // Verify User Notification
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(message.From, It.Is<string>(s => s.Contains("Ocurrió un error"))), Times.Once);
    }
}
