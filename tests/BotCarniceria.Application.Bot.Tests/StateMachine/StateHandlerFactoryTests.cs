using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.StateMachine;
using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.Application.Bot.Tests.StateMachine;

public class StateHandlerFactoryTests
{
    private static IServiceProvider CreateMockServiceProvider()
    {
        var mockServiceProvider = new Mock<IServiceProvider>();
#pragma warning disable SYSLIB0050 // Type or member is obsolete
        mockServiceProvider.Setup(x => x.GetService(It.IsAny<Type>()))
            .Returns((Type t) => System.Runtime.Serialization.FormatterServices.GetUninitializedObject(t));
#pragma warning restore SYSLIB0050
        return mockServiceProvider.Object;
    }

    [Fact]
    public void GetHandler_WithStartState_ShouldReturnStartStateHandler()
    {
        // Arrange
        var factory = new StateHandlerFactory(CreateMockServiceProvider());

        // Act
        var handler = factory.GetHandler(ConversationState.START);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<StartStateHandler>();
    }

    [Fact]
    public void GetHandler_WithMenuState_ShouldReturnMenuStateHandler()
    {
        // Arrange
        var factory = new StateHandlerFactory(CreateMockServiceProvider());

        // Act
        var handler = factory.GetHandler(ConversationState.MENU);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<MenuStateHandler>();
    }

    [Fact]
    public void GetHandler_WithConfirmLateOrderState_ShouldReturnConfirmLateOrderStateHandler()
    {
        // Arrange
        var factory = new StateHandlerFactory(CreateMockServiceProvider());

        // Act
        var handler = factory.GetHandler(ConversationState.CONFIRM_LATE_ORDER);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<ConfirmLateOrderStateHandler>();
    }

    [Theory]
    [InlineData(ConversationState.BILLING_WARNING)]
    [InlineData(ConversationState.BILLING_CHECK_DATA)]
    [InlineData(ConversationState.BILLING_ASK_RAZON_SOCIAL)]
    [InlineData(ConversationState.BILLING_ASK_RFC)]
    [InlineData(ConversationState.BILLING_ASK_CALLE)]
    [InlineData(ConversationState.BILLING_ASK_NUMERO)]
    [InlineData(ConversationState.BILLING_ASK_COLONIA)]
    [InlineData(ConversationState.BILLING_ASK_CP)]
    [InlineData(ConversationState.BILLING_ASK_CORREO)]
    [InlineData(ConversationState.BILLING_ASK_REGIMEN)]
    [InlineData(ConversationState.BILLING_CONFIRM_DATA)]
    [InlineData(ConversationState.BILLING_ASK_NOTE_FOLIO)]
    [InlineData(ConversationState.BILLING_ASK_NOTE_TOTAL)]
    [InlineData(ConversationState.BILLING_ASK_CFDI)]
    public void GetHandler_WithBillingStates_ShouldReturnBillingStateHandler(ConversationState billingState)
    {
        // Arrange
        var factory = new StateHandlerFactory(CreateMockServiceProvider());

        // Act
        var handler = factory.GetHandler(billingState);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<BillingStateHandler>();
    }

    [Fact]
    public void GetHandler_WithUnknownState_ShouldReturnDefaultMenuStateHandler()
    {
        // Arrange
        var factory = new StateHandlerFactory(CreateMockServiceProvider());
        var unknownState = (ConversationState)9999;

        // Act
        var handler = factory.GetHandler(unknownState);

        // Assert
        handler.Should().NotBeNull();
        handler.Should().BeOfType<MenuStateHandler>();
    }

    [Fact]
    public void GetHandler_WithEveryKnownStateInEnum_ShouldReturnNonNullHandler()
    {
        // Arrange
        var factory = new StateHandlerFactory(CreateMockServiceProvider());
        var allStates = Enum.GetValues<ConversationState>();

        // Act & Assert
        foreach (var state in allStates)
        {
            var handler = factory.GetHandler(state);
            handler.Should().NotBeNull($"Handler for state '{state}' must not be null");
            handler.Should().BeAssignableTo<IConversationStateHandler>();
        }
    }
}
