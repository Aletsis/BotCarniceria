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

public class ClientesControllerTests
{
    private readonly Mock<IMediator> _mockMediator;
    private readonly ClientesController _controller;

    public ClientesControllerTests()
    {
        _mockMediator = new Mock<IMediator>();
        _controller = new ClientesController(_mockMediator.Object);
    }

    [Fact]
    public async Task Create_ShouldReturnOkWithResult()
    {
        // Arrange
        var command = new CreateOrUpdateClienteCommand { Nombre = "Test", NumeroTelefono = "123" };
        var expectedDto = new ClienteDto { ClienteID = 123, Nombre = "Test" };
        
        _mockMediator.Setup(m => m.Send(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDto);

        // Act
        var result = await _controller.Create(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(expectedDto);
    }

    [Fact]
    public async Task GetByPhone_ExistingClient_ShouldReturnOk()
    {
        // Arrange
        var phone = "123456";
        var clienteDto = new ClienteDto { ClienteID = 1, Nombre = "Test" };
        _mockMediator.Setup(m => m.Send(It.Is<GetClienteByPhoneQuery>(q => q.PhoneNumber == phone), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteDto);

        // Act
        var result = await _controller.GetByPhone(phone);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeEquivalentTo(clienteDto);
    }

    [Fact]
    public async Task GetByPhone_NonExistingClient_ShouldReturnNotFound()
    {
        // Arrange
        var phone = "000000";
        _mockMediator.Setup(m => m.Send(It.IsAny<GetClienteByPhoneQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClienteDto?)null);

        // Act
        var result = await _controller.GetByPhone(phone);

        // Assert
        result.Should().BeOfType<NotFoundResult>();
    }
}
