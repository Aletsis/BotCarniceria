using System.Reflection;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using BotCarniceria.Core.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BotCarniceria.UnitTests.Application.DTOs;

public class DtoMappingTests
{
    private static void SetPrivateProperty<T>(T instance, string propertyName, object value)
    {
        var prop = typeof(T).GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        if (prop != null && prop.CanWrite)
        {
            prop.SetValue(instance, value);
        }
        else
        {
            // Fallback for backing field if needed, but usually private setter is enough for GetProperty
            // If GetProperty fails or CanWrite is false (no setter), we might need backing field convention
            // But EF Core entities usually have private setters.
            var field = typeof(T).GetField($"<{propertyName}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(instance, value);
            }
        }
    }

    [Fact]
    public void ConversacionDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var conversacion = Conversacion.Create("1234567890", 15);
        conversacion.CambiarEstado(ConversationState.MENU);
        conversacion.GuardarBuffer("Buffer content");
        conversacion.GuardarNombreTemporal("TempName");
        // Update UltimaActividad explicitly if needed, but Create sets it.
        
        // Act
        var dto = ConversacionDto.FromEntity(conversacion);

        // Assert
        dto.NumeroTelefono.Should().Be(conversacion.NumeroTelefono);
        dto.Estado.Should().Be(conversacion.Estado.ToString());
        dto.Buffer.Should().Be(conversacion.Buffer);
        dto.NombreTemporal.Should().Be(conversacion.NombreTemporal);
        dto.TimeoutEnMinutos.Should().Be(conversacion.TimeoutEnMinutos);
        dto.UltimaActividad.Should().Be(conversacion.UltimaActividad);
        dto.EstaExpirada.Should().Be(conversacion.EstaExpirada());
    }

    [Fact]
    public void PedidoDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var cliente = Cliente.Create("5555555555", "Cliente Test", "Calle Falsa 123");
        SetPrivateProperty(cliente, "ClienteID", 1);

        var pedido = Pedido.Create(cliente.ClienteID, "2kg de carne", "Sin notas", "Efectivo");
        SetPrivateProperty(pedido, "PedidoID", 100L);
        SetPrivateProperty(pedido, "Folio", Folio.From("CAR-20230101-ABCD"));
        
        // Use reflection to set Cliente navigation property as it's not set by Create
        SetPrivateProperty(pedido, "Cliente", cliente);

        // Act
        var dto = PedidoDto.FromEntity(pedido);

        // Assert
        dto.PedidoID.Should().Be(pedido.PedidoID);
        dto.Folio.Should().Be(pedido.Folio.Value);
        dto.Estado.Should().Be(pedido.Estado.ToString());
        dto.ClienteID.Should().Be(pedido.ClienteID);
        dto.ClienteNombre.Should().Be(cliente.Nombre);
        dto.ClienteTelefono.Should().Be(cliente.NumeroTelefono);
        dto.ClienteDireccion.Should().Be(cliente.Direccion);
        dto.Contenido.Should().Be(pedido.Contenido);
        dto.Fecha.Should().Be(pedido.Fecha);
        dto.FormaPago.Should().Be(pedido.FormaPago);
        dto.Notas.Should().Be(pedido.Notas);
        dto.EstadoImpresion.Should().Be(pedido.EstadoImpresion);
        dto.FechaImpresion.Should().Be(pedido.FechaImpresion);
    }

    [Fact]
    public void PedidoDto_FromEntity_ShouldHandleNullCliente()
    {
        // Arrange
        var pedido = Pedido.Create(2, "Test content");
        SetPrivateProperty(pedido, "PedidoID", 101L);
        // Cliente is null by default

        // Act
        var dto = PedidoDto.FromEntity(pedido);

        // Assert
        dto.ClienteNombre.Should().BeEmpty();
        dto.ClienteTelefono.Should().BeEmpty();
        dto.ClienteDireccion.Should().BeEmpty();
    }

    [Fact]
    public void ClienteDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var cliente = Cliente.Create("9876543210", "Otro Cliente", "Avenida Siempre Viva");
        SetPrivateProperty(cliente, "ClienteID", 5);

        // Add pedidos to verify TotalPedidos and UltimoPedidoFecha
        var pedido1 = Pedido.Create(5, "P1");
        SetPrivateProperty(pedido1, "Fecha", DateTime.UtcNow.AddDays(-1));
        var pedido2 = Pedido.Create(5, "P2");
        SetPrivateProperty(pedido2, "Fecha", DateTime.UtcNow);

        // Use reflection to add to _pedidos private collection
        var pedidosField = typeof(Cliente).GetField("_pedidos", BindingFlags.NonPublic | BindingFlags.Instance);
        if (pedidosField != null)
        {
            var list = (List<Pedido>)pedidosField.GetValue(cliente)!;
            list.Add(pedido1);
            list.Add(pedido2);
        }

        // Act
        var dto = ClienteDto.FromEntity(cliente);

        // Assert
        dto.ClienteID.Should().Be(cliente.ClienteID);
        dto.NumeroTelefono.Should().Be(cliente.NumeroTelefono);
        dto.Nombre.Should().Be(cliente.Nombre);
        dto.Direccion.Should().Be(cliente.Direccion);
        dto.Activo.Should().Be(cliente.Activo);
        dto.TotalPedidos.Should().Be(2);
        dto.UltimoPedidoFecha.Should().Be(pedido2.Fecha);
    }

    [Fact]
    public void ConfiguracionDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var config = Configuracion.Create("TestKey", "TestValue", TipoConfiguracion.Texto, "Test Description", true);
        SetPrivateProperty(config, "ConfigID", 10);

        // Act
        var dto = ConfiguracionDto.FromEntity(config);

        // Assert
        dto.ConfigID.Should().Be(config.ConfigID);
        dto.Clave.Should().Be(config.Clave);
        dto.Valor.Should().Be(config.Valor);
        dto.Tipo.Should().Be(config.Tipo.ToString());
        dto.Descripcion.Should().Be(config.Descripcion);
        dto.Editable.Should().Be(config.Editable);
    }

    [Fact]
    public void UsuarioDto_FromEntity_ShouldMapCorrectly()
    {
        // Arrange
        var usuario = Usuario.Create("admin", "hash", "Administrator", RolUsuario.Admin, "5551234567");
        SetPrivateProperty(usuario, "UsuarioID", 1);

        // Act
        var dto = UsuarioDto.FromEntity(usuario);

        // Assert
        dto.UsuarioID.Should().Be(usuario.UsuarioID);
        dto.NombreUsuario.Should().Be(usuario.Username);
        dto.NombreCompleto.Should().Be(usuario.Nombre);
        dto.Rol.Should().Be(usuario.Rol.ToString());
        dto.Telefono.Should().Be(usuario.Telefono);
        dto.Activo.Should().Be(usuario.Activo);
    }
}
