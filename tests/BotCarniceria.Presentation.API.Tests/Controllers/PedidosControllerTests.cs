using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.API.Controllers;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BotCarniceria.Presentation.API.Tests.Controllers;

public class PedidosControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly PedidosController _controller;

    public PedidosControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new PedidosController(_mockMediator.Object);
    }

    [Fact]
    public async Task GetById_ExistingOrder_ShouldReturnOk()
    {
        // Arrange
        var id = 100L;
        var pedidoDto = new PedidoDto { PedidoID = id, Contenido = "Carnita" };
        _mockMediator.Setup(m => m.Send(It.Is<GetPedidoByIdQuery>(q => q.PedidoID == id), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pedidoDto);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(pedidoDto);
    }

    [Fact]
    public async Task GetById_NonExistingOrder_ShouldReturnNotFound()
    {
        // Arrange
        var id = 999L;
        _mockMediator.Setup(m => m.Send(It.IsAny<GetPedidoByIdQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PedidoDto?)null);

        // Act
        var result = await _controller.GetById(id);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Create_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var command = new CreatePedidoCommand { ClienteID = 1 };
        var createdPedido = new PedidoDto { PedidoID = 55, ClienteID = 1 };
        
        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdPedido);

        // Act
        var result = await _controller.Create(command);

        // Assert
        var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        createdResult.ActionName.Should().Be(nameof(PedidosController.GetById));
        createdResult.RouteValues?["id"].Should().Be(createdPedido.PedidoID);
        createdResult.Value.Should().BeEquivalentTo(createdPedido);
    }
}
