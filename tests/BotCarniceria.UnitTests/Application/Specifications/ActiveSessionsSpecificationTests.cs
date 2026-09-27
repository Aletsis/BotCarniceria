using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Specifications;

public class ActiveSessionsSpecificationTests
{
    [Fact]
    public void IsSatisfiedBy_WhenEstadoIsStart_ReturnsFalse()
    {
        // Arrange
        var session = Conversacion.Create("5551234567"); // Defaults to START
        var spec = new ActiveSessionsSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(session).Should().BeFalse();
    }

    [Theory]
    [InlineData(ConversationState.MENU)]
    [InlineData(ConversationState.TAKING_ORDER)]
    [InlineData(ConversationState.ASK_NAME)]
    [InlineData(ConversationState.ASK_ADDRESS)]
    [InlineData(ConversationState.CONFIRM_ADDRESS)]
    [InlineData(ConversationState.CONFIRM_LATE_ORDER)]
    [InlineData(ConversationState.BILLING_WARNING)]
    [InlineData(ConversationState.AWAITING_CONFIRM)]
    [InlineData(ConversationState.ADDING_MORE)]
    [InlineData(ConversationState.SELECT_PAYMENT)]
    public void IsSatisfiedBy_WhenEstadoIsNotStart_ReturnsTrue(ConversationState activeState)
    {
        // Arrange
        var session = Conversacion.Create("5551234567");
        session.CambiarEstado(activeState);
        var spec = new ActiveSessionsSpecification();

        // Act & Assert
        spec.IsSatisfiedBy(session).Should().BeTrue();
    }

    [Fact]
    public void ToExpression_WorksWithLinqWhere_FiltersOnlyActiveSessions()
    {
        // Arrange
        var s1 = Conversacion.Create("5551111111"); // START

        var s2 = Conversacion.Create("5552222222");
        s2.CambiarEstado(ConversationState.TAKING_ORDER);

        var s3 = Conversacion.Create("5553333333");
        s3.CambiarEstado(ConversationState.MENU);

        var s4 = Conversacion.Create("5554444444"); // START

        var sessions = new List<Conversacion> { s1, s2, s3, s4 };
        var spec = new ActiveSessionsSpecification();

        // Act
        var activeSessions = sessions.AsQueryable().Where(spec.ToExpression()).ToList();

        // Assert
        activeSessions.Should().HaveCount(2);
        activeSessions.Should().Contain(s2);
        activeSessions.Should().Contain(s3);
        activeSessions.Should().NotContain(s1);
        activeSessions.Should().NotContain(s4);
    }
}
