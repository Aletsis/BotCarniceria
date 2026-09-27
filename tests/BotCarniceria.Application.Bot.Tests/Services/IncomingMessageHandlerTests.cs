using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.Services;
using BotCarniceria.Core.Application.DTOs.WhatsApp;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
    private readonly Mock<IMessageRepository> _mockMessageRepo;
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
        _mockMessageRepo = new Mock<IMessageRepository>();

        _mockUnitOfWork.Setup(u => u.Sessions).Returns(_mockSessionRepo.Object);
        _mockUnitOfWork.Setup(u => u.Messages).Returns(_mockMessageRepo.Object);
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
    public async Task HandleAsync_EmptyPhoneNumber_ShouldReturnImmediately()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "",
            Id = "msg_123",
            Type = "text",
            Text = new WhatsAppText { Body = "Hola" }
        };

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockWhatsAppService.Verify(w => w.MarkMessageAsReadAsync(It.IsAny<string>()), Times.Never);
        _mockSessionRepo.Verify(r => r.GetByPhoneAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MarkAsReadThrows_ShouldLogWarningAndContinue()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Id = "msg_123",
            Type = "text",
            Text = new WhatsAppText { Body = "Hola" }
        };

        _mockWhatsAppService.Setup(w => w.MarkMessageAsReadAsync("msg_123"))
            .ThrowsAsync(new Exception("Network failure marking read"));

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not mark message as read")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        // Verify handler was still executed
        _mockHandler.Verify(h => h.HandleAsync(message.From, "Hola", TipoContenidoMensaje.Texto, session), Times.Once);
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
        _mockSessionRepo.Verify(r => r.AddAsync(It.Is<Conversacion>(c => c.NumeroTelefono == message.From)), Times.Once);
        _mockWhatsAppService.Verify(w => w.MarkMessageAsReadAsync(message.Id), Times.Once);
        _mockFactory.Verify(f => f.GetHandler(ConversationState.START), Times.Once);
        _mockHandler.Verify(h => h.HandleAsync(message.From, "Hola", TipoContenidoMensaje.Texto, It.IsAny<Conversacion>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.AtLeastOnce());
    }

    [Theory]
    [InlineData("menu")]
    [InlineData("cancelar")]
    [InlineData("inicio")]
    public async Task HandleAsync_GlobalCommands_ShouldResetStateToMenuAndClearBuffer(string command)
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "text",
            Text = new WhatsAppText { Body = command }
        };

        var session = Conversacion.Create(message.From);
        session.CambiarEstado(ConversationState.TAKING_ORDER);
        session.GuardarBuffer("Pedido anterior");

        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);

        // Act
        await _service.HandleAsync(message);

        // Assert
        session.Estado.Should().Be(ConversationState.MENU);
        session.Buffer.Should().BeNull();
        _mockFactory.Verify(f => f.GetHandler(ConversationState.MENU), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_InteractiveButtonMessage_ShouldExtractId()
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
    public async Task HandleAsync_InteractiveListMessage_ShouldExtractListReplyId()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "interactive",
            Interactive = new WhatsAppInteractive
            {
                List_Reply = new WhatsAppListReply { Id = "list_opt_1", Title = "Opcion 1", Description = "Desc" }
            }
        };

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockHandler.Verify(h => h.HandleAsync(message.From, "list_opt_1", TipoContenidoMensaje.Interactivo, session), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_LocationMessage_ShouldSaveAndSendApology()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "location",
            Location = new WhatsAppLocation { Latitude = 25.6866, Longitude = -100.3161, Address = "Monterrey, NL" }
        };

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);
        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(message.From, 20, 0))
            .ReturnsAsync(new List<Mensaje>());

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockMessageRepo.Verify(r => r.AddAsync(It.Is<Mensaje>(m =>
            m.NumeroTelefono == message.From &&
            m.TipoContenido == TipoContenidoMensaje.Ubicacion &&
            m.Contenido.Contains("25.6866") &&
            m.MetadataWhatsApp == "Monterrey, NL")), Times.Once);

        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            message.From,
            It.Is<string>(msg => msg.Contains("Ubicaciones"))),
            Times.Once);

        // Should not delegate to state handler
        _mockHandler.Verify(h => h.HandleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<TipoContenidoMensaje>(), It.IsAny<Conversacion>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_ContactsMessage_WithContact_ShouldExtractNameAndPhoneAndSendApology()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "contacts",
            Contacts = new List<WhatsAppMessageContact>
            {
                new WhatsAppMessageContact
                {
                    Name = new WhatsAppContactName { Formatted_Name = "Carlos Ruiz" },
                    Phones = new List<WhatsAppContactPhone> { new WhatsAppContactPhone { Phone = "+528111223344" } }
                }
            }
        };

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);
        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(message.From, 20, 0))
            .ReturnsAsync(new List<Mensaje>());

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockMessageRepo.Verify(r => r.AddAsync(It.Is<Mensaje>(m =>
            m.NumeroTelefono == message.From &&
            m.Contenido == "Carlos Ruiz (+528111223344)")), Times.Once);

        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            message.From,
            It.Is<string>(msg => msg.Contains("Contactos"))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ContactsMessage_EmptyList_ShouldFallbackToDefault()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "contacts",
            Contacts = new List<WhatsAppMessageContact>()
        };

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);
        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(message.From, 20, 0))
            .ReturnsAsync(new List<Mensaje>());

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockMessageRepo.Verify(r => r.AddAsync(It.Is<Mensaje>(m =>
            m.Contenido == "[Contacto]")), Times.Once);
    }

    [Theory]
    [InlineData("image", "Imágenes")]
    [InlineData("document", "Documentos")]
    [InlineData("audio", "Audios")]
    [InlineData("voice", "Audios")]
    [InlineData("sticker", "Stickers")]
    public async Task HandleAsync_MediaMessage_WithSuccessfulDownload_ShouldDownloadSaveMetadataAndApologize(string mediaType, string apologyKeyword)
    {
        // Arrange
        var media = new WhatsAppMedia { Id = "media_999", Caption = "Nota de audio/foto", Mime_Type = "image/jpeg" };
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = mediaType,
            Image = mediaType == "image" ? media : null,
            Document = mediaType == "document" ? media : null,
            Audio = (mediaType == "audio" || mediaType == "voice") ? media : null,
            Sticker = mediaType == "sticker" ? media : null
        };

        _mockWhatsAppService.Setup(w => w.DownloadMediaAsync("media_999"))
            .ReturnsAsync("/media/downloads/media_999.jpg");

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);
        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(message.From, 20, 0))
            .ReturnsAsync(new List<Mensaje>());

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockWhatsAppService.Verify(w => w.DownloadMediaAsync("media_999"), Times.Once);
        _mockMessageRepo.Verify(r => r.AddAsync(It.Is<Mensaje>(m =>
            m.Contenido == "/media/downloads/media_999.jpg" &&
            m.MetadataWhatsApp != null &&
            m.MetadataWhatsApp.Contains("media_999"))), Times.Once);

        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            message.From,
            It.Is<string>(msg => msg.Contains(apologyKeyword))),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_MediaMessage_WhenDownloadFails_ShouldSaveErrorMessage()
    {
        // Arrange
        var message = new WhatsAppMessage
        {
            From = "5551234567",
            Type = "image",
            Image = new WhatsAppMedia { Id = "media_fail" }
        };

        _mockWhatsAppService.Setup(w => w.DownloadMediaAsync("media_fail"))
            .ReturnsAsync((string?)null);

        var session = Conversacion.Create(message.From);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(message.From)).ReturnsAsync(session);
        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(message.From, 20, 0))
            .ReturnsAsync(new List<Mensaje>());

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockMessageRepo.Verify(r => r.AddAsync(It.Is<Mensaje>(m =>
            m.Contenido == "[Error descargando image]")), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnsupportedMessage_WhenLastOutgoingHasJsonMetadata_ShouldResendViaResendMessageAsync()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var message = new WhatsAppMessage
        {
            From = phoneNumber,
            Type = "sticker",
            Sticker = new WhatsAppMedia { Id = "stk_123" }
        };

        _mockWhatsAppService.Setup(w => w.DownloadMediaAsync("stk_123"))
            .ReturnsAsync("/media/stk_123.webp");

        var session = Conversacion.Create(phoneNumber);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(phoneNumber)).ReturnsAsync(session);

        var outgoingMsg = Mensaje.CrearSaliente(phoneNumber, "Menu anterior", TipoContenidoMensaje.Interactivo);
        outgoingMsg.SetMetadata("{\"type\":\"interactive\",\"action\":\"menu\"}");

        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(phoneNumber, 20, 0))
            .ReturnsAsync(new List<Mensaje> { outgoingMsg });

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(phoneNumber, It.Is<string>(msg => msg.Contains("Stickers"))), Times.Once);
        _mockWhatsAppService.Verify(w => w.ResendMessageAsync(phoneNumber, outgoingMsg.MetadataWhatsApp!), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_UnsupportedMessage_WhenLastOutgoingHasTextContentOnly_ShouldResendViaSendTextMessageAsync()
    {
        // Arrange
        var phoneNumber = "5551234567";
        var message = new WhatsAppMessage
        {
            From = phoneNumber,
            Type = "audio",
            Audio = new WhatsAppMedia { Id = "aud_123" }
        };

        _mockWhatsAppService.Setup(w => w.DownloadMediaAsync("aud_123"))
            .ReturnsAsync("/media/aud_123.mp3");

        var session = Conversacion.Create(phoneNumber);
        _mockSessionRepo.Setup(r => r.GetByPhoneAsync(phoneNumber)).ReturnsAsync(session);

        var outgoingMsg = Mensaje.CrearSaliente(phoneNumber, "¿Cuál es tu nombre completo?", TipoContenidoMensaje.Texto);

        _mockMessageRepo.Setup(r => r.GetByPhoneAsync(phoneNumber, 20, 0))
            .ReturnsAsync(new List<Mensaje> { outgoingMsg });

        // Act
        await _service.HandleAsync(message);

        // Assert
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(phoneNumber, It.Is<string>(msg => msg.Contains("Audios"))), Times.Once);
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(phoneNumber, "¿Cuál es tu nombre completo?"), Times.Once);
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
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(message.From, It.Is<string>(s => s.Contains("Ocurrió un error"))), Times.Once);
    }
}
