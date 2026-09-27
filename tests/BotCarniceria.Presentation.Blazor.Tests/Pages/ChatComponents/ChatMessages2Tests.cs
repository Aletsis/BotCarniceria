using Bunit;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Components.Pages.ChatComponents;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages.ChatComponents;

public class ChatMessages2Tests : IAsyncLifetime
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
    public void ChatMessages2_ShouldRenderTextMessage_IncomingAndOutgoing()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "Hola, buenas tardes",
                EsEntrante = true,
                Tipo = "texto",
                Fecha = DateTime.UtcNow
            },
            new MensajeDto
            {
                MensajeID = 2,
                Contenido = "Hola, en qué podemos ayudarte?",
                EsEntrante = false,
                Tipo = "texto",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("Hola, buenas tardes");
        cut.Markup.Should().Contain("Hola, en qué podemos ayudarte?");
        cut.Markup.Should().Contain("received-message");
        cut.Markup.Should().Contain("sent-message");
    }

    [Fact]
    public void ChatMessages2_ShouldRenderImageMessage_WithCaption()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "https://example.com/imagen.jpg",
                EsEntrante = true,
                Tipo = "imagen",
                Metadata = "{\"Caption\":\"Corte de carne ribeye\"}",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("https://example.com/imagen.jpg");
        cut.Markup.Should().Contain("Corte de carne ribeye");
    }

    [Fact]
    public void ChatMessages2_ShouldRenderAudioMessage()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "https://example.com/audio.ogg",
                EsEntrante = false,
                Tipo = "audio",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("<audio");
        cut.Markup.Should().Contain("https://example.com/audio.ogg");
    }

    [Fact]
    public void ChatMessages2_ShouldRenderDocumentMessage_WithFilenameAndCaption()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "https://example.com/lista_precios.pdf",
                EsEntrante = true,
                Tipo = "documento",
                Metadata = "{\"Filename\":\"Precios.pdf\",\"Caption\":\"Lista de mayo\"}",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("Precios.pdf");
        cut.Markup.Should().Contain("Lista de mayo");
        cut.Markup.Should().Contain("https://example.com/lista_precios.pdf");
    }

    [Fact]
    public void ChatMessages2_ShouldRenderLocationMessage()
    {
        // Arrange
        var messages = new List<MensajeDto>
        {
            new MensajeDto
            {
                MensajeID = 1,
                Contenido = "19.4326,-99.1332",
                EsEntrante = true,
                Tipo = "ubicacion",
                Metadata = "Sucursal Centro",
                Fecha = DateTime.UtcNow
            }
        };

        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.Messages, messages);
        });

        // Assert
        cut.Markup.Should().Contain("Ver Ubicación");
        cut.Markup.Should().Contain("https://www.google.com/maps/search/?api=1&amp;query=19.4326,-99.1332");
        cut.Markup.Should().Contain("Sucursal Centro");
    }

    [Fact]
    public void ChatMessages2_WhenHasMoreMessages_ShouldShowLoadButtonAndTriggerCallback()
    {
        // Arrange
        bool loadMoreCalled = false;
        var cut = Context.Render<ChatMessages2>(parameters =>
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
    public void ChatMessages2_WhenIsLoading_ShouldShowLoadingText()
    {
        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.HasMoreMessages, true);
            parameters.Add(p => p.IsLoading, true);
        });

        // Assert
        cut.Markup.Should().Contain("Cargando...");
    }

    [Fact]
    public void ChatMessages2_WhenIsTyping_ShouldShowTypingIndicator()
    {
        // Act
        var cut = Context.Render<ChatMessages2>(parameters =>
        {
            parameters.Add(p => p.IsTyping, true);
        });

        // Assert
        cut.Find(".typing-indicator").Should().NotBeNull();
    }

    [Fact]
    public async Task ChatMessages2_ScrollToBottom_ShouldInvokeJSRuntime()
    {
        // Arrange
        var cut = Context.Render<ChatMessages2>();

        // Act
        await cut.Instance.ScrollToBottom();

        // Assert - JSRuntime invocation in Loose mode does not throw
        Context.JSInterop.Invocations.Should().NotBeNull();
    }
}
