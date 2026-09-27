using Bunit;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Components.Pages.ChatComponents;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages.ChatComponents;

public class ChatMessagesTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void ChatMessages_ShouldRenderTextMessage_IncomingAndOutgoing()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "¿Cuál es el precio del bistec?",
                EsEntrante = true,
                Tipo = "texto",
                Fecha = DateTime.UtcNow
            },
            new MensajeDto
            {
                MensajeID = 2,
                Contenido = "El precio es de $180 el kilo",
                EsEntrante = false,
                Tipo = "texto",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("¿Cuál es el precio del bistec?");
        cut.Markup.Should().Contain("El precio es de $180 el kilo");
        cut.FindComponents<MudBlazor.MudChat>().Should().HaveCount(2);
    }

    [Fact]
    public void ChatMessages_ShouldRenderImageMessage_WithCaption()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "https://example.com/corte.jpg",
                EsEntrante = true,
                Tipo = "imagen",
                Metadata = "{\"Caption\":\"Corte New York\"}",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("https://example.com/corte.jpg");
        cut.Markup.Should().Contain("Corte New York");
    }

    [Fact]
    public void ChatMessages_ShouldRenderAudioMessage()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "https://example.com/audio.mp3",
                EsEntrante = false,
                Tipo = "audio",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("<audio");
        cut.Markup.Should().Contain("https://example.com/audio.mp3");
    }

    [Fact]
    public void ChatMessages_ShouldRenderDocumentMessage_WithFilenameAndCaption()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "https://example.com/factura.pdf",
                EsEntrante = true,
                Tipo = "documento",
                Metadata = "{\"Filename\":\"Factura_123.pdf\",\"Caption\":\"Factura de compra\"}",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("Factura_123.pdf");
        cut.Markup.Should().Contain("Factura de compra");
        cut.Markup.Should().Contain("https://example.com/factura.pdf");
    }

    [Fact]
    public void ChatMessages_ShouldRenderLocationMessage()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "20.6736,-103.3440",
                EsEntrante = true,
                Tipo = "ubicacion",
                Metadata = "Sucursal Guadalajara",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("Ver Ubicación");
        cut.Markup.Should().Contain("https://www.google.com/maps/search/?api=1&amp;query=20.6736,-103.3440");
        cut.Markup.Should().Contain("Sucursal Guadalajara");
    }

    [Fact]
    public void ChatMessages_WhenHasMoreMessages_ShouldShowLoadButtonAndTriggerCallback()
    {
        // Arrange
        bool loadMoreCalled = false;
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.HasMoreMessages, true);
            parameters.Add(p => p.IsLoading, false);
            parameters.Add(p => p.OnLoadMore, () => { loadMoreCalled = true; });
        });

        // Act
        cut.Markup.Should().Contain("Cargar anteriores");
        var button = cut.FindAll("button").First(b => b.TextContent.Contains("Cargar anteriores"));
        button.Click();

        // Assert
        loadMoreCalled.Should().BeTrue();
    }

    [Fact]
    public void ChatMessages_WhenIsLoading_ShouldShowLoadingText()
    {
        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.HasMoreMessages, true);
            parameters.Add(p => p.IsLoading, true);
        });

        // Assert
        cut.Markup.Should().Contain("Cargando...");
    }

    [Fact]
    public void ChatMessages_WhenIsTyping_ShouldShowTypingIndicator()
    {
        // Act
        var cut = Context.Render<ChatMessages>(parameters =>
        {
            parameters.Add(p => p.IsTyping, true);
        });

        // Assert
        cut.Find(".typing-indicator").Should().NotBeNull();
    }

    [Fact]
    public async Task ChatMessages_ScrollToBottom_ShouldInvokeJSRuntime()
    {
        // Arrange
        var cut = Context.Render<ChatMessages>();

        // Act
        await cut.Instance.ScrollToBottom();

        // Assert
        Context.JSInterop.Invocations.Should().NotBeNull();
    }
}
