using Bunit;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class EstadosTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<ISender> _mockSender;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public EstadosTests()
    {
        _mockSender = new Mock<ISender>();
        _mockSnackbar = new Mock<ISnackbar>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddSingleton(_mockSender.Object);
        Context.Services.AddSingleton(_mockSnackbar.Object);
        Context.JSInterop.Mode = JSRuntimeMode.Loose;

        Context.Render<MudPopoverProvider>();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await Context.DisposeAsync();
    }

    [Fact]
    public void Estados_ShouldRenderCorrectly()
    {
        // Act
        var cut = Context.Render<Estados>();

        // Assert
        cut.Markup.Should().Contain("Subir Estado");
        cut.Markup.Should().Contain("Seleccionar Archivo (Imagen/Video)");
        cut.Markup.Should().Contain("Comentario / Caption (Opcional)");
    }

    [Fact]
    public async Task Estados_UploadFile_Success_ShouldCallSenderAndShowSuccessAlert()
    {
        // Arrange
        _mockSender.Setup(s => s.Send(It.IsAny<UploadEstadoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("media-id-999");

        var cut = Context.Render<Estados>();

        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("oferta.jpg");
        mockFile.Setup(f => f.Size).Returns(2048);
        mockFile.Setup(f => f.ContentType).Returns("image/jpeg");
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(new byte[] { 1, 2, 3 }));

        // Act - Simulate file upload event
        var fileUpload = cut.FindComponent<MudFileUpload<IBrowserFile>>();
        await cut.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(mockFile.Object));

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("oferta.jpg (2 KB)"));

        // Click Subir Estado button
        var uploadButton = cut.FindAll("button").First(b => b.TextContent.Contains("Subir Estado"));
        await cut.InvokeAsync(() => uploadButton.Click());

        // Assert
        _mockSender.Verify(s => s.Send(It.Is<UploadEstadoCommand>(c => c.FileName == "oferta.jpg" && c.ContentType == "image/jpeg"), It.IsAny<CancellationToken>()), Times.Once);
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Estado subido exitosamente. ID de Media: media-id-999"));
        _mockSnackbar.Verify(s => s.Add("Estado subido correctamente a WhatsApp", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Estados_UploadFile_WhenSenderReturnsNull_ShouldShowErrorSnackbar()
    {
        // Arrange
        _mockSender.Setup(s => s.Send(It.IsAny<UploadEstadoCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var cut = Context.Render<Estados>();

        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("error.png");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(new byte[] { 1 }));

        var fileUpload = cut.FindComponent<MudFileUpload<IBrowserFile>>();
        await cut.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(mockFile.Object));

        // Act
        var uploadButton = cut.FindAll("button").First(b => b.TextContent.Contains("Subir Estado"));
        await cut.InvokeAsync(() => uploadButton.Click());

        // Assert
        _mockSnackbar.Verify(s => s.Add("Error al subir estado", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Estados_UploadFile_WhenExceptionThrown_ShouldShowExceptionSnackbar()
    {
        // Arrange
        _mockSender.Setup(s => s.Send(It.IsAny<UploadEstadoCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Falló la conexión con WhatsApp"));

        var cut = Context.Render<Estados>();

        var mockFile = new Mock<IBrowserFile>();
        mockFile.Setup(f => f.Name).Returns("test.png");
        mockFile.Setup(f => f.Size).Returns(1024);
        mockFile.Setup(f => f.ContentType).Returns("image/png");
        mockFile.Setup(f => f.OpenReadStream(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .Returns(new MemoryStream(new byte[] { 1 }));

        var fileUpload = cut.FindComponent<MudFileUpload<IBrowserFile>>();
        await cut.InvokeAsync(() => fileUpload.Instance.FilesChanged.InvokeAsync(mockFile.Object));

        // Act
        var uploadButton = cut.FindAll("button").First(b => b.TextContent.Contains("Subir Estado"));
        await cut.InvokeAsync(() => uploadButton.Click());

        // Assert
        _mockSnackbar.Verify(s => s.Add("Error: Falló la conexión con WhatsApp", Severity.Error, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }
}
