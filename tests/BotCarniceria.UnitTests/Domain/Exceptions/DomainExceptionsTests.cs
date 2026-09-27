using BotCarniceria.Core.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Domain.Exceptions;

public class DomainExceptionsTests
{
    private class TestDomainException : DomainException
    {
        public TestDomainException(string message) : base(message) { }
        public TestDomainException(string message, Exception innerException) : base(message, innerException) { }
    }

    #region DomainValidationException Tests

    [Fact]
    public void DomainValidationException_Constructor_SetsMessageAndInheritsFromDomainException()
    {
        // Arrange
        const string errorMessage = "El teléfono no tiene un formato válido.";

        // Act
        var exception = new DomainValidationException(errorMessage);

        // Assert
        exception.Message.Should().Be(errorMessage);
        exception.Should().BeAssignableTo<DomainException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void DomainValidationException_CanBeThrownAndCaughtAsDomainException()
    {
        // Arrange
        Action act = () => throw new DomainValidationException("Error de validación");

        // Act & Assert
        act.Should().Throw<DomainValidationException>()
            .WithMessage("Error de validación");

        act.Should().Throw<DomainException>();
    }

    #endregion

    #region EntityNotFoundDomainException Tests

    [Fact]
    public void EntityNotFoundDomainException_Constructor_WithNumericKey_FormatsMessageCorrectly()
    {
        // Arrange
        const string entityName = "Pedido";
        const long key = 12345L;

        // Act
        var exception = new EntityNotFoundDomainException(entityName, key);

        // Assert
        exception.Message.Should().Be("Entity 'Pedido' with key '12345' was not found.");
        exception.Should().BeAssignableTo<DomainException>();
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void EntityNotFoundDomainException_Constructor_WithStringKey_FormatsMessageCorrectly()
    {
        // Arrange
        const string entityName = "Configuracion";
        const string key = "Printer_Name";

        // Act
        var exception = new EntityNotFoundDomainException(entityName, key);

        // Assert
        exception.Message.Should().Be("Entity 'Configuracion' with key 'Printer_Name' was not found.");
    }

    [Fact]
    public void EntityNotFoundDomainException_CanBeThrownAndCaughtAsDomainException()
    {
        // Arrange
        Action act = () => throw new EntityNotFoundDomainException("Cliente", 999);

        // Act & Assert
        act.Should().Throw<EntityNotFoundDomainException>()
            .WithMessage("Entity 'Cliente' with key '999' was not found.");

        act.Should().Throw<DomainException>();
    }

    #endregion

    #region Additional Domain Exceptions Tests

    [Fact]
    public void InvalidDomainOperationException_Constructor_SetsMessage()
    {
        // Arrange
        const string errorMessage = "No se puede cancelar un pedido entregado.";

        // Act
        var exception = new InvalidDomainOperationException(errorMessage);

        // Assert
        exception.Message.Should().Be(errorMessage);
        exception.Should().BeAssignableTo<DomainException>();
    }

    [Fact]
    public void DomainException_WithInnerException_SetsMessageAndInnerException()
    {
        // Arrange
        var inner = new InvalidOperationException("Fallo interno");
        const string message = "Error de dominio con causa interna";

        // Act
        var exception = new TestDomainException(message, inner);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(inner);
    }

    #endregion
}
