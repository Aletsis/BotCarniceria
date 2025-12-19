using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Domain.Entities;

public class ConversacionTests
{
    [Fact]
    public void Create_ShouldCreateConversacionWithCorrectProperties()
    {
        // Arrange
        var numeroTelefono = "5551234567";
        var timeoutMinutes = 30;

        // Act
        var conversacion = Conversacion.Create(numeroTelefono, timeoutMinutes);

        // Assert
        conversacion.Should().NotBeNull();
        conversacion.NumeroTelefono.Should().Be(numeroTelefono);
        conversacion.Estado.Should().Be(ConversationState.START);
        conversacion.TimeoutEnMinutos.Should().Be(timeoutMinutes);
        conversacion.UltimaActividad.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        conversacion.Buffer.Should().BeNull();
        conversacion.NombreTemporal.Should().BeNull();
    }

    [Fact]
    public void Create_WithDefaultTimeout_ShouldUse30Minutes()
    {
        // Arrange
        var numeroTelefono = "5551234567";

        // Act
        var conversacion = Conversacion.Create(numeroTelefono);

        // Assert
        conversacion.TimeoutEnMinutos.Should().Be(30);
    }

    [Fact]
    public void Create_WithEmptyTelefono_ShouldThrowArgumentException()
    {
        // Arrange
        var numeroTelefono = "";

        // Act
        Action act = () => Conversacion.Create(numeroTelefono);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Teléfono requerido");
    }

    [Fact]
    public void Create_WithWhitespaceTelefono_ShouldThrowArgumentException()
    {
        // Arrange
        var numeroTelefono = "   ";

        // Act
        Action act = () => Conversacion.Create(numeroTelefono);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("Teléfono requerido");
    }

    [Fact]
    public void ActualizarActividad_ShouldUpdateUltimaActividad()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        var actividadInicial = conversacion.UltimaActividad;

        // Act
        System.Threading.Thread.Sleep(100);
        conversacion.ActualizarActividad();

        // Assert
        conversacion.UltimaActividad.Should().BeAfter(actividadInicial);
    }

    [Fact]
    public void CambiarEstado_ShouldUpdateEstadoAndUltimaActividad()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        var actividadInicial = conversacion.UltimaActividad;
        var nuevoEstado = ConversationState.MENU;

        // Act
        System.Threading.Thread.Sleep(100);
        conversacion.CambiarEstado(nuevoEstado);

        // Assert
        conversacion.Estado.Should().Be(nuevoEstado);
        conversacion.UltimaActividad.Should().BeAfter(actividadInicial);
    }

    [Theory]
    [InlineData(ConversationState.START)]
    [InlineData(ConversationState.MENU)]
    [InlineData(ConversationState.ASK_NAME)]
    [InlineData(ConversationState.ASK_ADDRESS)]
    [InlineData(ConversationState.TAKING_ORDER)]
    [InlineData(ConversationState.SELECT_PAYMENT)]
    [InlineData(ConversationState.AWAITING_CONFIRM)]
    [InlineData(ConversationState.CONFIRM_ADDRESS)]
    [InlineData(ConversationState.ADDING_MORE)]
    public void CambiarEstado_WithAllStates_ShouldUpdateCorrectly(ConversationState estado)
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");

        // Act
        conversacion.CambiarEstado(estado);

        // Assert
        conversacion.Estado.Should().Be(estado);
    }

    [Fact]
    public void GuardarBuffer_ShouldSaveDataAndUpdateActividad()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        var actividadInicial = conversacion.UltimaActividad;
        var dato = "2 kg de carne molida";

        // Act
        System.Threading.Thread.Sleep(100);
        conversacion.GuardarBuffer(dato);

        // Assert
        conversacion.Buffer.Should().Be(dato);
        conversacion.UltimaActividad.Should().BeAfter(actividadInicial);
    }

    [Fact]
    public void GuardarBuffer_CalledMultipleTimes_ShouldOverwritePreviousData()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        var primerDato = "Primer dato";
        var segundoDato = "Segundo dato";

        // Act
        conversacion.GuardarBuffer(primerDato);
        conversacion.GuardarBuffer(segundoDato);

        // Assert
        conversacion.Buffer.Should().Be(segundoDato);
    }

    [Fact]
    public void LimpiarBuffer_ShouldSetBufferToNull()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        conversacion.GuardarBuffer("Algún dato");

        // Act
        conversacion.LimpiarBuffer();

        // Assert
        conversacion.Buffer.Should().BeNull();
    }

    [Fact]
    public void GuardarNombreTemporal_ShouldSaveNameAndUpdateActividad()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        var actividadInicial = conversacion.UltimaActividad;
        var nombre = "Juan Pérez";

        // Act
        System.Threading.Thread.Sleep(100);
        conversacion.GuardarNombreTemporal(nombre);

        // Assert
        conversacion.NombreTemporal.Should().Be(nombre);
        conversacion.UltimaActividad.Should().BeAfter(actividadInicial);
    }

    [Fact]
    public void FechaExpiracion_ShouldBeCalculatedCorrectly()
    {
        // Arrange
        var timeoutMinutes = 30;
        var conversacion = Conversacion.Create("5551234567", timeoutMinutes);

        // Act
        var fechaExpiracion = conversacion.FechaExpiracion;

        // Assert
        fechaExpiracion.Should().NotBeNull();
        fechaExpiracion.Should().BeCloseTo(
            conversacion.UltimaActividad.AddMinutes(timeoutMinutes), 
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void EstaExpirada_WhenNotExpired_ShouldReturnFalse()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567", 30);

        // Act
        var estaExpirada = conversacion.EstaExpirada();

        // Assert
        estaExpirada.Should().BeFalse();
    }

    [Fact]
    public void EstaExpirada_WhenExpired_ShouldReturnTrue()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567", 0); // 0 minutos de timeout
        System.Threading.Thread.Sleep(100); // Esperar un poco para que expire

        // Act
        var estaExpirada = conversacion.EstaExpirada();

        // Assert
        estaExpirada.Should().BeTrue();
    }

    [Fact]
    public void EstaExpirada_AfterActualizarActividad_ShouldReturnFalse()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567", 1); // 1 minuto de timeout
        System.Threading.Thread.Sleep(100);

        // Act
        conversacion.ActualizarActividad(); // Resetea el timeout
        var estaExpirada = conversacion.EstaExpirada();

        // Assert
        estaExpirada.Should().BeFalse();
    }

    [Fact]
    public void ConversationFlow_ShouldMaintainStateCorrectly()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");

        // Act & Assert - Simular un flujo de conversación
        conversacion.Estado.Should().Be(ConversationState.START);

        conversacion.CambiarEstado(ConversationState.MENU);
        conversacion.Estado.Should().Be(ConversationState.MENU);

        conversacion.CambiarEstado(ConversationState.ASK_NAME);
        conversacion.Estado.Should().Be(ConversationState.ASK_NAME);

        conversacion.GuardarNombreTemporal("Juan Pérez");
        conversacion.NombreTemporal.Should().Be("Juan Pérez");

        conversacion.CambiarEstado(ConversationState.ASK_ADDRESS);
        conversacion.Estado.Should().Be(ConversationState.ASK_ADDRESS);

        conversacion.GuardarBuffer("Calle Principal 123");
        conversacion.Buffer.Should().Be("Calle Principal 123");

        conversacion.CambiarEstado(ConversationState.TAKING_ORDER);
        conversacion.Estado.Should().Be(ConversationState.TAKING_ORDER);
    }

    [Fact]
    public void Create_WithCustomTimeout_ShouldUseProvidedValue()
    {
        // Arrange
        var timeoutMinutes = 60;

        // Act
        var conversacion = Conversacion.Create("5551234567", timeoutMinutes);

        // Assert
        conversacion.TimeoutEnMinutos.Should().Be(timeoutMinutes);
        conversacion.FechaExpiracion.Should().BeCloseTo(
            DateTime.UtcNow.AddMinutes(timeoutMinutes), 
            TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BufferAndNombreTemporal_ShouldBeIndependent()
    {
        // Arrange
        var conversacion = Conversacion.Create("5551234567");
        var nombre = "Juan Pérez";
        var buffer = "Datos del pedido";

        // Act
        conversacion.GuardarNombreTemporal(nombre);
        conversacion.GuardarBuffer(buffer);

        // Assert
        conversacion.NombreTemporal.Should().Be(nombre);
        conversacion.Buffer.Should().Be(buffer);

        // Act - Limpiar buffer no debe afectar nombre temporal
        conversacion.LimpiarBuffer();

        // Assert
        conversacion.NombreTemporal.Should().Be(nombre);
        conversacion.Buffer.Should().BeNull();
    }
}
