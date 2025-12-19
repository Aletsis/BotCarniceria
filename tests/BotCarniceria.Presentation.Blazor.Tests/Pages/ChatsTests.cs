using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MudBlazor;
using MudBlazor.Services;
using MediatR;
using FluentAssertions;
using Microsoft.AspNetCore.Components;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class ChatsTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;

    public ChatsTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        Context.Services.AddSingleton(_mockMediator.Object);
        // Chats.razor no parece usar AuthenticationStateProvider explícitamente en el @code ni @inject, 
        // pero NavigationManager y ISnackbar sí.
        
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        // Render Providers
        Context.Render<MudPopoverProvider>();
        Context.Render<MudDialogProvider>();
        Context.Render<MudSnackbarProvider>();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void ChatsPage_ShouldLoadChatsList()
    {
        // Arrange
        var chats = new List<ChatSummaryDto> 
        { 
            new ChatSummaryDto 
            { 
                Nombre = "Pepe Argento", 
                NumeroTelefono = "1234567890", 
                LastActivity = DateTime.Now 
            } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetActiveChatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chats);

        // Act
        var cut = Context.Render<Chats>();

        // Assert
        cut.WaitForState(() => cut.FindAll(".mud-list-item").Count > 0);
        cut.Markup.Should().Contain("Pepe Argento");
    }

    [Fact]
    public void ChatsPage_ShouldFilterChats()
    {
        // Arrange
        var chats = new List<ChatSummaryDto> 
        { 
            new ChatSummaryDto { Nombre = "Pepe Argento", NumeroTelefono = "123", LastActivity = DateTime.Now },
            new ChatSummaryDto { Nombre = "Moni Argento", NumeroTelefono = "456", LastActivity = DateTime.Now }
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetActiveChatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chats);

        var cut = Context.Render<Chats>();
        cut.WaitForState(() => cut.FindAll(".mud-list-item").Count == 2);

        // Act
        var searchInput = cut.FindAll("input").FirstOrDefault(i => i.GetAttribute("placeholder")?.Contains("Buscar") == true);
        searchInput?.Input("Moni");

        // Assert
        cut.WaitForState(() => cut.FindAll(".mud-list-item").Count == 1);
        cut.Markup.Should().Contain("Moni Argento");
        cut.Markup.Should().NotContain("Pepe Argento");
    }

    [Fact]
    public void ChatsPage_ShouldSelectChatAndLoadMessages()
    {
        // Arrange
        var chat = new ChatSummaryDto { Nombre = "Pepe", NumeroTelefono = "123", LastActivity = DateTime.Now };
        var chats = new List<ChatSummaryDto> { chat };
        var messages = new List<MensajeDto> 
        { 
            new MensajeDto { Contenido = "Hola", EsEntrante = true, Fecha = DateTime.Now } 
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetActiveChatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chats);
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetChatMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        var cut = Context.Render<Chats>();
        cut.WaitForState(() => cut.FindAll(".mud-list-item").Count > 0);

        // Act
        cut.Find(".mud-list-item").Click();

        // Assert
        // Verify messages are loaded
        cut.WaitForState(() => cut.Markup.Contains("Hola"));
        
        // Check if message query was sent
        _mockMediator.Verify(m => m.Send(It.Is<GetChatMessagesQuery>(q => q.PhoneNumber == "123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ChatsPage_ShouldSendMessage()
    {
        // Arrange
        var chat = new ChatSummaryDto { Nombre = "Pepe", NumeroTelefono = "123", LastActivity = DateTime.Now };
        var chats = new List<ChatSummaryDto> { chat };
        var messages = new List<MensajeDto>(); // Start with empty messages

        _mockMediator.Setup(m => m.Send(It.IsAny<GetActiveChatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(chats);
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetChatMessagesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messages);

        _mockMediator.Setup(m => m.Send(It.IsAny<SendWhatsAppMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var cut = Context.Render<Chats>();
        cut.WaitForState(() => cut.FindAll(".mud-list-item").Count > 0);
        
        // Select Chat first
        cut.Find(".mud-list-item").Click();

        // Wait for loading to finish and input to appear
        cut.WaitForState(() => cut.FindAll("div.mud-progress-circular").Count == 0);
        cut.WaitForState(() => cut.FindAll("input").Any(i => i.GetAttribute("placeholder")?.Contains("Escribe un mensaje") == true));
        
        // Act
        // Find input by placeholder
        var messageInput = cut.FindAll("input").FirstOrDefault(i => i.GetAttribute("placeholder")?.Contains("Escribe un mensaje") == true);
        if (messageInput == null) throw new Exception("Message input not found!");
        
        messageInput.Input("Hola mundo");
        
        // Wait for button to be enabled
        cut.WaitForState(() => 
        {
             var btn = cut.FindComponents<MudIconButton>()
                .FirstOrDefault(b => b.Instance.Icon == Icons.Material.Filled.Send);
             return btn != null && !btn.Find("button").HasAttribute("disabled");
        });

        var sendButton = cut.FindComponents<MudIconButton>()
            .FirstOrDefault(b => b.Instance.Icon == Icons.Material.Filled.Send);

        sendButton?.Find("button").Click();

        // Assert
        _mockMediator.Verify(m => m.Send(It.Is<SendWhatsAppMessageCommand>(c => c.PhoneNumber == "123" && c.Message == "Hola mundo"), It.IsAny<CancellationToken>()), Times.Once);
    }
}
