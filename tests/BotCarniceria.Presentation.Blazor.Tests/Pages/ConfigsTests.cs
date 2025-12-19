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
using Microsoft.AspNetCore.Components;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class ConfigsTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;

    public ConfigsTests()
    {
        _mockMediator = new Mock<IMediator>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        
        Context.Services.AddSingleton(_mockMediator.Object);
        // Inject IDialogService required by Configs
        Context.Services.AddSingleton(new Mock<IDialogService>().Object);
        
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
    public void ConfigsPage_ShouldLoadAndShowConfigs()
    {
        // Arrange
        var configs = new List<ConfiguracionDto> 
        { 
            new ConfiguracionDto 
            { 
                Clave = "TestKey", 
                Valor = "TestValue", 
                Descripcion = "Description", 
                Tipo = "String", 
                Editable = true 
            } 
        };
        
        _mockMediator.Setup(m => m.Send(It.IsAny<GetAllConfiguracionesQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(configs);

        // Act
        var cut = Context.Render<Configs>();

        // Assert
        cut.WaitForState(() => cut.FindAll("tr").Count >= 2);
        cut.Markup.Should().Contain("TestKey");
        cut.Markup.Should().Contain("TestValue");
    }
}
