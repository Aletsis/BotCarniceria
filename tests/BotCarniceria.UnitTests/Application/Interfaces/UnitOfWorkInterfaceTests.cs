using BotCarniceria.Core.Application.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.Interfaces;

public class UnitOfWorkInterfaceTests
{
    private class TestUnitOfWork : IUnitOfWork
    {
        public IOrderRepository Orders { get; set; } = null!;
        public IClienteRepository Clientes { get; set; } = null!;
        public ISessionRepository Sessions { get; set; } = null!;
        public IMessageRepository Messages { get; set; } = null!;
        public IConfiguracionRepository Settings { get; set; } = null!;
        public IUsuarioRepository Users { get; set; } = null!;
        public ISolicitudFacturaRepository SolicitudesFactura { get; set; } = null!;

        public bool SaveChangesCalled { get; private set; }
        public bool BeginTransactionCalled { get; private set; }
        public bool CommitTransactionCalled { get; private set; }
        public bool RollbackTransactionCalled { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.FromResult(1);
        }

        public Task BeginTransactionAsync()
        {
            BeginTransactionCalled = true;
            return Task.CompletedTask;
        }

        public Task CommitTransactionAsync()
        {
            CommitTransactionCalled = true;
            return Task.CompletedTask;
        }

        public Task RollbackTransactionAsync()
        {
            RollbackTransactionCalled = true;
            return Task.CompletedTask;
        }
    }

    [Fact]
    public void Configuraciones_DefaultInterfaceImplementation_ReturnsSettingsInstance()
    {
        // Arrange
        var mockSettings = new Mock<IConfiguracionRepository>();
        var testUow = new TestUnitOfWork
        {
            Settings = mockSettings.Object
        };

        // Act - Call through IUnitOfWork interface to invoke default interface member
        IUnitOfWork uow = testUow;
        var configuraciones = uow.Configuraciones;

        // Assert
        configuraciones.Should().NotBeNull();
        configuraciones.Should().BeSameAs(mockSettings.Object);
        configuraciones.Should().BeSameAs(uow.Settings);
    }

    [Fact]
    public async Task UnitOfWork_PropertiesAndMethods_CanBeAccessedAndInvoked()
    {
        // Arrange
        var mockOrders = new Mock<IOrderRepository>();
        var mockClientes = new Mock<IClienteRepository>();
        var mockSessions = new Mock<ISessionRepository>();
        var mockMessages = new Mock<IMessageRepository>();
        var mockSettings = new Mock<IConfiguracionRepository>();
        var mockUsers = new Mock<IUsuarioRepository>();
        var mockSolicitudes = new Mock<ISolicitudFacturaRepository>();

        var testUow = new TestUnitOfWork
        {
            Orders = mockOrders.Object,
            Clientes = mockClientes.Object,
            Sessions = mockSessions.Object,
            Messages = mockMessages.Object,
            Settings = mockSettings.Object,
            Users = mockUsers.Object,
            SolicitudesFactura = mockSolicitudes.Object
        };

        IUnitOfWork uow = testUow;

        // Act
        var saveResult = await uow.SaveChangesAsync();
        await uow.BeginTransactionAsync();
        await uow.CommitTransactionAsync();
        await uow.RollbackTransactionAsync();

        // Assert
        uow.Orders.Should().BeSameAs(mockOrders.Object);
        uow.Clientes.Should().BeSameAs(mockClientes.Object);
        uow.Sessions.Should().BeSameAs(mockSessions.Object);
        uow.Messages.Should().BeSameAs(mockMessages.Object);
        uow.Settings.Should().BeSameAs(mockSettings.Object);
        uow.Users.Should().BeSameAs(mockUsers.Object);
        uow.SolicitudesFactura.Should().BeSameAs(mockSolicitudes.Object);

        saveResult.Should().Be(1);
        testUow.SaveChangesCalled.Should().BeTrue();
        testUow.BeginTransactionCalled.Should().BeTrue();
        testUow.CommitTransactionCalled.Should().BeTrue();
        testUow.RollbackTransactionCalled.Should().BeTrue();
    }
}
