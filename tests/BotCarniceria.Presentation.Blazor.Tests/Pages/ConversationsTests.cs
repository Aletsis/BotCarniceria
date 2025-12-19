using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using MudBlazor;
using MudBlazor.Services;
using MediatR;
using FluentAssertions;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class ConversationsTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;

    public ConversationsTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        Context.Services.AddSingleton(_mockMediator.Object);
        // Providers
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
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
    public void ConversationsPage_ShouldLoadList()
    {
        // Arrange
        var list = new List<ConversacionDto> 
        { 
            new ConversacionDto { NumeroTelefono = "123", Estado = "Active", NombreTemporal = "Temp", UltimaActividad = DateTime.Now } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConversationsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(list);

        // Act
        var cut = Context.Render<Conversations>();

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);
        cut.Markup.Should().Contain("123");
        cut.Markup.Should().Contain("Temp");
    }
}
