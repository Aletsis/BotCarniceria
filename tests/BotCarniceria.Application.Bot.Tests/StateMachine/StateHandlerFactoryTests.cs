using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.StateMachine;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine;

public class StateHandlerFactoryTests
{
    [Fact]
    public void GetHandler_WithStartState_ShouldReturnHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        
#pragma warning disable SYSLIB0050 // Type or member is obsolete
        mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type t) => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t));
#pragma warning restore SYSLIB0050 // Type or member is obsolete

        var factory = new StateHandlerFactory(mockServiceProvider.Object);

        // Act
        var handler = factory.GetHandler(ConversationState.START);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeAssignableTo<IConversationStateHandler>();
    }

    [Fact]
    public void GetHandler_WithMenuState_ShouldReturnHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        
#pragma warning disable SYSLIB0050
        mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type t) => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t));
#pragma warning restore SYSLIB0050

        var factory = new StateHandlerFactory(mockServiceProvider.Object);

        // Act
        var handler = factory.GetHandler(ConversationState.MENU);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeAssignableTo<IConversationStateHandler>();
    }

    [Fact]
    public void GetHandler_WithSelectPaymentState_ShouldReturnHandler()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        
#pragma warning disable SYSLIB0050
        mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type t) => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t));
#pragma warning restore SYSLIB0050

        var factory = new StateHandlerFactory(mockServiceProvider.Object);

        // Act
        var handler = factory.GetHandler(ConversationState.SELECT_PAYMENT);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeAssignableTo<IConversationStateHandler>();
    }

    [Fact]
    public void GetHandler_WithAllKnownStates_ShouldReturnHandlers()
    {
        // Arrange
        var mockServiceProvider = new Mock<IServiceProvider>();
        
#pragma warning disable SYSLIB0050
        mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type t) => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t));
#pragma warning restore SYSLIB0050

        var factory = new StateHandlerFactory(mockServiceProvider.Object);

        var states = new[]
        {
            ConversationState.START,
            ConversationState.MENU,
            ConversationState.ASK_NAME,
            ConversationState.ASK_ADDRESS,
            ConversationState.TAKING_ORDER,
            ConversationState.ADDING_MORE,
            ConversationState.AWAITING_CONFIRM,
            ConversationState.CONFIRM_ADDRESS,
            ConversationState.SELECT_PAYMENT
        };

        // Act & Assert
        foreach (var state in states)
        {
            var handler = factory.GetHandler(state);
            handler.Should().NotBeNull($"Handler for state {state} should not be null");
            handler.Should().BeAssignableTo<IConversationStateHandler>();
        }
    }
}
