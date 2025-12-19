using BotCarniceria.Core.Application.CQRS.Handlers;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Specifications;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace BotCarniceria.UnitTests.Application.CQRS.Handlers;

public class PedidoQueryHandlersTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IOrderRepository> _mockPedidoRepository;

    public PedidoQueryHandlersTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockPedidoRepository = new Mock<IOrderRepository>();
        _mockUnitOfWork.Setup(x => x.Orders).Returns(_mockPedidoRepository.Object);
    }

    #region GetPedidoByIdQueryHandler Tests

    [Fact]
    public async Task GetPedidoByIdQueryHandler_WhenPedidoExists_ShouldReturnPedidoDto()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido", "Test notas", "Efectivo");
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync(pedido);

        var query = new GetPedidoByIdQuery { PedidoID = 1 };
        var handler = new GetPedidoByIdQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contenido.Should().Be("Test contenido");
        result.Notas.Should().Be("Test notas");
        result.FormaPago.Should().Be("Efectivo");
    }

    [Fact]
    public async Task GetPedidoByIdQueryHandler_WhenPedidoNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.GetByIdAsync(It.IsAny<long>()))
            .ReturnsAsync((Pedido?)null);

        var query = new GetPedidoByIdQuery { PedidoID = 999 };
        var handler = new GetPedidoByIdQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetPedidoByFolioQueryHandler Tests

    [Fact]
    public async Task GetPedidoByFolioQueryHandler_WhenPedidoExists_ShouldReturnPedidoDto()
    {
        // Arrange
        var pedido = Pedido.Create(1, "Test contenido");
        var pedidos = new List<Pedido> { pedido };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new GetPedidoByFolioQuery { Folio = pedido.Folio.Value };
        var handler = new GetPedidoByFolioQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Contenido.Should().Be("Test contenido");
    }

    [Fact]
    public async Task GetPedidoByFolioQueryHandler_WhenPedidoNotFound_ShouldReturnNull()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido>());

        var query = new GetPedidoByFolioQuery { Folio = "NONEXISTENT" };
        var handler = new GetPedidoByFolioQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllPedidosQueryHandler Tests

    [Fact]
    public async Task GetAllPedidosQueryHandler_ShouldReturnAllPedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(2, "Pedido 2"),
            Pedido.Create(3, "Pedido 3")
        };

        _mockPedidoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(pedidos);

        var query = new GetAllPedidosQuery();
        var handler = new GetAllPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.Contenido == "Pedido 1");
        result.Should().Contain(p => p.Contenido == "Pedido 2");
        result.Should().Contain(p => p.Contenido == "Pedido 3");
    }

    [Fact]
    public async Task GetAllPedidosQueryHandler_WhenNoPedidos_ShouldReturnEmptyList()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(new List<Pedido>());

        var query = new GetAllPedidosQuery();
        var handler = new GetAllPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPedidosByClienteQueryHandler Tests

    [Fact]
    public async Task GetPedidosByClienteQueryHandler_ShouldReturnClientePedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(1, "Pedido 2")
        };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new GetPedidosByClienteQuery { ClienteID = 1 };
        var handler = new GetPedidosByClienteQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.ClienteID.Should().Be(1));
    }

    #endregion

    #region GetPendingPedidosQueryHandler Tests

    [Fact]
    public async Task GetPendingPedidosQueryHandler_ShouldReturnOnlyPendingPedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(2, "Pedido 2")
        };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new GetPendingPedidosQuery();
        var handler = new GetPendingPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.Estado.Should().Be(EstadoPedido.EnEspera.ToString()));
    }

    #endregion

    #region GetTodayPedidosQueryHandler Tests

    [Fact]
    public async Task GetTodayPedidosQueryHandler_ShouldReturnTodaysPedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido hoy 1"),
            Pedido.Create(2, "Pedido hoy 2")
        };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new GetTodayPedidosQuery();
        var handler = new GetTodayPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => 
            p.Fecha.Date.Should().Be(DateTime.UtcNow.Date));
    }

    #endregion

    #region GetActiveClientePedidosQueryHandler Tests

    [Fact]
    public async Task GetActiveClientePedidosQueryHandler_ShouldReturnActiveClientePedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido activo 1"),
            Pedido.Create(1, "Pedido activo 2")
        };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new GetActiveClientePedidosQuery { ClienteID = 1 };
        var handler = new GetActiveClientePedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => p.ClienteID.Should().Be(1));
    }

    #endregion

    #region SearchPedidosQueryHandler Tests

    [Fact]
    public async Task SearchPedidosQueryHandler_ShouldReturnMatchingPedidos()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Carne molida"),
            Pedido.Create(2, "Carne de res")
        };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new SearchPedidosQuery { SearchTerm = "carne" };
        var handler = new SearchPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p => 
            p.Contenido.Should().Contain("Carne", "carne"));
    }

    [Fact]
    public async Task SearchPedidosQueryHandler_WithEmptySearchTerm_ShouldReturnEmptyList()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido>());

        var query = new SearchPedidosQuery { SearchTerm = "" };
        var handler = new SearchPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region GetPedidosByDateRangeQueryHandler Tests

    [Fact]
    public async Task GetPedidosByDateRangeQueryHandler_ShouldReturnPedidosInRange()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(-7);
        var endDate = DateTime.UtcNow.AddDays(1); // Add buffer for test execution time
        
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(2, "Pedido 2")
        };

        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(pedidos);

        var query = new GetPedidosByDateRangeQuery 
        { 
            StartDate = startDate,
            EndDate = endDate
        };
        var handler = new GetPedidosByDateRangeQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(p =>
        {
            p.Fecha.Should().BeOnOrAfter(startDate);
            p.Fecha.Should().BeOnOrBefore(endDate);
        });
    }

    #endregion

    #region GetFilteredPedidosQueryHandler Tests

    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithNoFilters_ShouldReturnAllPedidos_AndCallGetAllAsync()
    {
        // Arrange
        var pedidos = new List<Pedido>
        {
            Pedido.Create(1, "Pedido 1"),
            Pedido.Create(2, "Pedido 2"),
            Pedido.Create(3, "Pedido 3")
        };

        // When no filters are provided, the handler calls GetAllAsync
        _mockPedidoRepository.Setup(x => x.GetAllAsync())
            .ReturnsAsync(pedidos);

        var query = new GetFilteredPedidosQuery();
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);
        _mockPedidoRepository.Verify(x => x.GetAllAsync(), Times.Once);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Never);
    }

    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithOnlyToday_ShouldCallFindAsync()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido> { Pedido.Create(1, "Today Order") });

        var query = new GetFilteredPedidosQuery
        {
            OnlyToday = true
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Once);
        _mockPedidoRepository.Verify(x => x.GetAllAsync(), Times.Never);
    }

    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithDate_ShouldCallFindAsync()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido> { Pedido.Create(1, "Date Order") });

        var query = new GetFilteredPedidosQuery
        {
            Date = DateTime.UtcNow.Date
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Once);
    }

    [Theory]
    [InlineData("EnEspera", EstadoPedido.EnEspera)]
    [InlineData("EnRuta", EstadoPedido.EnRuta)]
    [InlineData("Entregado", EstadoPedido.Entregado)]
    [InlineData("Cancelado", EstadoPedido.Cancelado)]
    public async Task GetFilteredPedidosQueryHandler_WithValidEnumString_ShouldCallFindAsync(string estadoString, EstadoPedido expectedEnum)
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido> { Pedido.Create(1, "Status Order") });

        var query = new GetFilteredPedidosQuery
        {
            Estado = estadoString
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Once);
    }

    [Theory]
    [InlineData("En espera de surtir", EstadoPedido.EnEspera)]
    [InlineData("En espera", EstadoPedido.EnEspera)]
    [InlineData("En ruta", EstadoPedido.EnRuta)]
    public async Task GetFilteredPedidosQueryHandler_WithLegacyMapping_ShouldCallFindAsync(string legacyEstado, EstadoPedido expectedEnum)
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido> { Pedido.Create(1, "Legacy Status Order") });

        var query = new GetFilteredPedidosQuery
        {
            Estado = legacyEstado
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Once);
    }
    
    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithUnknownEstado_ShouldNotApplyEstadoFilter_ButStillCallGetAllIfNoOtherFilters()
    {
        // Arrange
        // If Estado is invalid and logic falls through switch default (null), spec remains null (if no other filters).
        // Then it should call GetAllAsync.
        var pedidos = new List<Pedido> { Pedido.Create(1, "Order") };
        _mockPedidoRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(pedidos);

        var query = new GetFilteredPedidosQuery
        {
            Estado = "EstadoInvalido"
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.GetAllAsync(), Times.Once);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Never);
    }

    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithTodosEstado_ShouldIgoreFilter()
    {
        // Arrange
        // "Todos" should skip the filter logic block.
        var pedidos = new List<Pedido> { Pedido.Create(1, "Order") };
        _mockPedidoRepository.Setup(x => x.GetAllAsync()).ReturnsAsync(pedidos);

        var query = new GetFilteredPedidosQuery
        {
            Estado = "Todos"
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithSearchTerm_ShouldCallFindAsync()
    {
        // Arrange
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido> { Pedido.Create(1, "Search Result") });

        var query = new GetFilteredPedidosQuery
        {
            SearchTerm = "something"
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Once);
    }

    [Fact]
    public async Task GetFilteredPedidosQueryHandler_WithMulitpleFilters_ShouldComposeSpecAndCallFindAsync()
    {
        // Arrange
        // Tests combination of filters: Date + Estado + Search
        _mockPedidoRepository.Setup(x => x.FindAsync(It.IsAny<Specification<Pedido>>()))
            .ReturnsAsync(new List<Pedido> { Pedido.Create(1, "Combined Result") });

        var query = new GetFilteredPedidosQuery
        {
            Date = DateTime.UtcNow.Date,
            Estado = "EnEspera",
            SearchTerm = "Test"
        };
        var handler = new GetFilteredPedidosQueryHandler(_mockUnitOfWork.Object);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);
        _mockPedidoRepository.Verify(x => x.FindAsync(It.IsAny<Specification<Pedido>>()), Times.Once);
    }

    #endregion
}
