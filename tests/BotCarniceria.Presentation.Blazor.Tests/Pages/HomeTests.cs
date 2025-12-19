using Bunit;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MudBlazor.Services;
using FluentAssertions;
using Moq;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class HomeTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices(); // MudCard/MudButton used
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Context.Services.AddSingleton(new Moq.Mock<MediatR.IMediator>().Object);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void Home_ShouldRenderDashboard()
    {
        var mockMediator = new Moq.Mock<MediatR.IMediator>();
        mockMediator.Setup(m => m.Send(It.IsAny<BotCarniceria.Core.Application.CQRS.Queries.GetDashboardStatsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BotCarniceria.Core.Application.DTOs.DashboardStatsDto());
            
        Context.Services.AddSingleton(mockMediator.Object);

        var cut = Context.Render<Home>();
        cut.Markup.Should().Contain("Dashboard");
        // Verify stats are rendered
        cut.Markup.Should().Contain("Pedidos Hoy");
    }
}
