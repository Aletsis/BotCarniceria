using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using Moq;
using Xunit;
using FluentAssertions;

namespace BotCarniceria.Application.Bot.Tests.StateMachine.Handlers;

public class AskAddressStateHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IWhatsAppService> _mockWhatsAppService;
    private readonly Mock<IClienteRepository> _mockClienteRepo;
    private readonly AskAddressStateHandler _handler;

    public AskAddressStateHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockWhatsAppService = new Mock<IWhatsAppService>();
        _mockClienteRepo = new Mock<IClienteRepository>();

        // Setup UnitOfWork to return our mock repo
        _mockUnitOfWork.Setup(u => u.Clientes).Returns(_mockClienteRepo.Object);

        _handler = new AskAddressStateHandler(_mockUnitOfWork.Object, _mockWhatsAppService.Object);
    }

    [Fact]
    public async Task HandleAsync_NewClient_ShouldCreateClientAndTransitionToTakingOrder()
    {
        // Arrange
        var phoneNumber = "5550001111";
        var address = "Calle Nueva 123";
        var session = Conversacion.Create(phoneNumber);
        // We assume NombreTemporal was set in previous step (AskName)
        // Since Conversacion properties are private set, we might rely on internal state or just assume 'Sin nombre' fallback if not set.
        // But wait, NombreTemporal is set via reflection or we simulate a state where it was set?
        // Actually Conversacion logic is domain. Reflection is easiest for test setup if setter is private.
        // Or we use a helper method if available. Let's assume it's null for now, code handles fallback.
        
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync((Cliente?)null);

        // Act
        await _handler.HandleAsync(phoneNumber, address, session);

        // Assert
        // Verify transaction flow
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify Client creation
        _mockClienteRepo.Verify(r => r.AddAsync(It.Is<Cliente>(c => 
            c.NumeroTelefono == phoneNumber && 
            c.Direccion == address
        )), Times.Once);

        // Verify State Transition
        session.Estado.Should().Be(ConversationState.TAKING_ORDER);

        // Verify WhatsApp Message (Normal flow)
        _mockWhatsAppService.Verify(w => w.SendTextMessageAsync(
            phoneNumber, 
            It.Is<string>(s => s.Contains("Perfecto") && s.Contains("escribir tu pedido"))
        ), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ExistingClient_ShouldUpdateAddressAndTransitionToTakingOrder()
    {
        // Arrange
        var phoneNumber = "5550002222";
        var newAddress = "Avenida Siempre Viva 742";
        var session = Conversacion.Create(phoneNumber);
        
        var existingCliente = Cliente.Create(phoneNumber, "Homero");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(existingCliente);

        // Act
        await _handler.HandleAsync(phoneNumber, newAddress, session);

        // Assert
        // Verify Client Update
        _mockUnitOfWork.Verify(u => u.Clientes.UpdateAsync(It.Is<Cliente>(c => c.Direccion == newAddress)), Times.Once);

        // Verify State
        session.Estado.Should().Be(ConversationState.TAKING_ORDER);
    }

    [Fact]
    public async Task HandleAsync_WithBuffer_ShouldTransitionToSelectPayment()
    {
        // Arrange
        var phoneNumber = "5550003333";
        var address = "Address update";
        var session = Conversacion.Create(phoneNumber);
        session.GuardarBuffer("PEDIDO PENDIENTE"); // Simulate buffer present

        var existingCliente = Cliente.Create(phoneNumber, "Marge");
        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(phoneNumber))
            .ReturnsAsync(existingCliente);

        // Act
        await _handler.HandleAsync(phoneNumber, address, session);

        // Assert
        session.Estado.Should().Be(ConversationState.SELECT_PAYMENT);

        // Verify Specific Warning/Confirmation Message
        _mockWhatsAppService.Verify(w => w.SendInteractiveButtonsAsync(
            phoneNumber,
            It.Is<string>(s => s.Contains("Dirección actualizada") && s.Contains("Cómo deseas pagar")),
            It.Is<List<(string, string)>>(l => l.Count == 2),
            It.IsAny<string?>(),
            It.IsAny<string?>()
        ), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_OnError_ShouldRollbackTransaction()
    {
        // Arrange
        var phoneNumber = "5550004444";
        var session = Conversacion.Create(phoneNumber);

        _mockClienteRepo.Setup(r => r.GetByPhoneAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Database connection failed"));

        // Act
        Func<Task> act = async () => await _handler.HandleAsync(phoneNumber, "Address", session);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Database connection failed");
        
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.RollbackTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Never);
    }
}
