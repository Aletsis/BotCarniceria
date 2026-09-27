using System.Net;
using System.Net.Sockets;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Services;
using BotCarniceria.Infrastructure.Services.External.Printing;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BotCarniceria.Infrastructure.Tests.Services.External.Printing;

public class PrintingServiceTests
{
    private readonly Mock<ILogger<PrintingService>> _mockLogger;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IDateTimeProvider> _mockDateTimeProvider;
    private readonly PrintingService _service;

    public PrintingServiceTests()
    {
        _mockLogger = new Mock<ILogger<PrintingService>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockDateTimeProvider = new Mock<IDateTimeProvider>();
        _mockDateTimeProvider.Setup(d => d.Now).Returns(new DateTime(2026, 9, 27, 10, 0, 0));

        _service = new PrintingService(_mockLogger.Object, _mockUnitOfWork.Object, _mockDateTimeProvider.Object);
    }

    [Fact]
    public async Task PrintTicketAsync_WhenPrinterReachable_ShouldSendBytesAndReturnTrue()
    {
        // Arrange - Start local loopback TCP listener on random port
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync((string?)null);
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.IpAddress))
            .ReturnsAsync("127.0.0.1");
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.Port))
            .ReturnsAsync(port.ToString());

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            var buffer = new byte[2048];
            int read = await stream.ReadAsync(buffer, 0, buffer.Length);
            return read;
        });

        try
        {
            // Act
            var result = await _service.PrintTicketAsync("FOL-100", "Juan Pérez", "5551234567", "Calle 1", "2 kg arrachera", "Sin notas");

            // Assert
            result.Should().BeTrue();
            var bytesRead = await serverTask;
            bytesRead.Should().BeGreaterThan(0);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task PrintTicketAsync_WithAdvancedJsonConfig_ShouldParseAndPrintSuccessfully()
    {
        // Arrange
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;

        var json = $"{{\"DefaultPrinterName\":\"TermicaPrincipal\",\"Printers\":[{{\"Name\":\"TermicaPrincipal\",\"IpAddress\":\"127.0.0.1\",\"Port\":{port}}}]}}";
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync(json);

        var serverTask = Task.Run(async () =>
        {
            using var client = await listener.AcceptTcpClientAsync();
            using var stream = client.GetStream();
            var buffer = new byte[2048];
            return await stream.ReadAsync(buffer, 0, buffer.Length);
        });

        try
        {
            // Act
            var result = await _service.PrintTicketAsync("FOL-200", "Maria Gomez", "5559876543", "Av. Hidalgo 20", "1 kg chorizo", "Bien dorado");

            // Assert
            result.Should().BeTrue();
            var bytesRead = await serverTask;
            bytesRead.Should().BeGreaterThan(0);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task PrintTicketAsync_WhenConnectionFails_ShouldReturnFalseAndLogError()
    {
        // Arrange - Unreachable IP and port
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.Configuration))
            .ReturnsAsync((string?)null);
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.IpAddress))
            .ReturnsAsync("127.0.0.1");
        _mockUnitOfWork.Setup(x => x.Settings.GetValorAsync(ConfigurationKeys.Printers.Port))
            .ReturnsAsync("59999"); // Closed port

        // Act
        var result = await _service.PrintTicketAsync("FOL-ERR", "Test", "123", "Dir", "Contenido", "");

        // Assert
        result.Should().BeFalse();
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error printing ticket FOL-ERR")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
