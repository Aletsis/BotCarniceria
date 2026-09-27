using Bunit;
using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Presentation.Blazor.Components.Pages;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace BotCarniceria.Presentation.Blazor.Tests.Pages;

public class SolicitudFacturaPublicaTests : IAsyncLifetime
{
    private BunitContext Context { get; set; } = default!;
    private readonly Mock<IMediator> _mockMediator;
    private readonly Mock<ISnackbar> _mockSnackbar;

    public SolicitudFacturaPublicaTests()
    {
        _mockMediator = new Mock<IMediator>();
        _mockSnackbar = new Mock<ISnackbar>();
    }

    public Task InitializeAsync()
    {
        Context = new BunitContext();
        Context.Services.AddMudServices();
        Context.Services.AddSingleton(_mockMediator.Object);
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
    public void SolicitudFacturaPublica_InitialRender_ShouldShowRfcStep()
    {
        // Act
        var cut = Context.Render<SolicitudFacturaPublica>();

        // Assert
        cut.Markup.Should().Contain("Solicitud de Factura");
        cut.Markup.Should().Contain("Ingresa tu RFC para solicitar tu factura electrónica");
        cut.Markup.Should().Contain("Buscar");
    }

    [Fact]
    public async Task SolicitudFacturaPublica_SearchExistingClient_ShouldTransitionToMostrarDatosWithClientInfo()
    {
        // Arrange
        var clienteDto = new ClienteDto
        {
            ClienteID = 10,
            Nombre = "Carnes del Norte",
            NumeroTelefono = "5551234567",
            DatosFacturacion = new DatosFacturacionDto
            {
                RazonSocial = "Carnes del Norte SA de CV",
                RFC = "CNO123456789",
                Calle = "Av Principal",
                Numero = "100",
                Colonia = "Centro",
                CodigoPostal = "64000",
                Correo = "facturas@carnes.com",
                RegimenFiscal = "601"
            }
        };

        _mockMediator.Setup(m => m.Send(It.Is<GetClienteByRFCQuery>(q => q.RFC == "CNO123456789"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteDto);

        var cut = Context.Render<SolicitudFacturaPublica>();

        // Act - Set RFC on MudTextField and click Search
        var rfcField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => rfcField.Instance.ValueChanged.InvokeAsync("CNO123456789"));

        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("¡Cliente encontrado!"));
        cut.Markup.Should().Contain("Carnes del Norte SA de CV");
        cut.Markup.Should().Contain("facturas@carnes.com");
    }

    [Fact]
    public async Task SolicitudFacturaPublica_SearchNonExistingClient_ShouldTransitionToMostrarDatosWithEmptyFields()
    {
        // Arrange
        _mockMediator.Setup(m => m.Send(It.IsAny<GetClienteByRFCQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ClienteDto?)null);

        var cut = Context.Render<SolicitudFacturaPublica>();

        // Act - Set RFC on MudTextField and click Search
        var rfcField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => rfcField.Instance.ValueChanged.InvokeAsync("NUEVO1234567"));

        var searchButton = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar"));
        await cut.InvokeAsync(() => searchButton.Click());

        // Assert
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Por favor ingresa tus datos de facturación para continuar."));
        _mockSnackbar.Verify(s => s.Add("RFC no encontrado. Por favor ingresa tus datos para registrarte.", Severity.Info, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SolicitudFacturaPublica_FullSubmissionFlow_ExistingClient_ShouldCompleteSuccessfully()
    {
        // Arrange
        var clienteDto = new ClienteDto
        {
            ClienteID = 25,
            Nombre = "Restaurante El Asador",
            NumeroTelefono = "5559876543",
            DatosFacturacion = new DatosFacturacionDto
            {
                RazonSocial = "El Asador SA",
                RFC = "ASA990101XYZ",
                Calle = "Hidalgo",
                Numero = "45",
                Colonia = "Juarez",
                CodigoPostal = "06600",
                Correo = "admin@elasador.com",
                RegimenFiscal = "601"
            }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetClienteByRFCQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteDto);
        _mockMediator.Setup(m => m.Send(It.IsAny<UpdateClienteDatosFacturacionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockMediator.Setup(m => m.Send(It.IsAny<CreateSolicitudFacturaCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(888L);

        var cut = Context.Render<SolicitudFacturaPublica>();

        // Step 1: Search RFC
        var rfcField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => rfcField.Instance.ValueChanged.InvokeAsync("ASA990101XYZ"));
        var searchBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar"));
        await cut.InvokeAsync(() => searchBtn.Click());

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("El Asador SA"));

        // Step 2: Continue to Datos Nota
        var continueBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Continuar"));
        await cut.InvokeAsync(() => continueBtn.Click());

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Ingresa los datos de tu ticket de compra"));

        // Step 3: Fill ticket info
        var folioField = cut.FindComponents<MudTextField<string>>().First(f => f.Instance.Label == "Folio del Ticket");
        await cut.InvokeAsync(() => folioField.Instance.ValueChanged.InvokeAsync("TICK-777"));

        var numericField = cut.FindComponent<MudNumericField<decimal>>();
        await cut.InvokeAsync(() => numericField.Instance.ValueChanged.InvokeAsync(1250.00m));

        var usoSelect = cut.FindComponent<MudSelect<string>>();
        await cut.InvokeAsync(() => usoSelect.Instance.ValueChanged.InvokeAsync("G01"));

        // Click Enviar Solicitud (run task in background since EnviarSolicitud starts countdown loop)
        var sendBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Enviar Solicitud"));
        _ = cut.InvokeAsync(() => sendBtn.Click());

        // Assert - transitions to confirmation
        cut.WaitForAssertion(() => cut.Markup.Should().Contain("¡Solicitud Enviada!"));
        cut.Markup.Should().Contain("#888");

        _mockMediator.Verify(m => m.Send(It.IsAny<CreateSolicitudFacturaCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockSnackbar.Verify(s => s.Add("¡Solicitud de factura enviada exitosamente!", Severity.Success, It.IsAny<Action<SnackbarOptions>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SolicitudFacturaPublica_Cancelar_ShouldReturnToPaso1()
    {
        // Arrange
        var clienteDto = new ClienteDto
        {
            ClienteID = 10,
            Nombre = "Carnes",
            NumeroTelefono = "5551234567",
            DatosFacturacion = new DatosFacturacionDto
            {
                RazonSocial = "Carnes SA",
                RFC = "CAR123456789",
                Calle = "Calle 1",
                Numero = "1",
                Colonia = "Col",
                CodigoPostal = "12345",
                Correo = "c@c.com",
                RegimenFiscal = "601"
            }
        };

        _mockMediator.Setup(m => m.Send(It.IsAny<GetClienteByRFCQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(clienteDto);

        var cut = Context.Render<SolicitudFacturaPublica>();
        var rfcField = cut.FindComponent<MudTextField<string>>();
        await cut.InvokeAsync(() => rfcField.Instance.ValueChanged.InvokeAsync("CAR123456789"));
        var searchBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Buscar"));
        await cut.InvokeAsync(() => searchBtn.Click());

        cut.WaitForAssertion(() => cut.Markup.Should().Contain("Carnes SA"));

        // Act - Click Regresar on Step 2
        var returnBtn = cut.FindAll("button").First(b => b.TextContent.Contains("Regresar"));
        await cut.InvokeAsync(() => returnBtn.Click());

        // Assert - Back to Step 1
        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Ingresa tu RFC para solicitar tu factura electrónica");
            cut.Markup.Should().NotContain("Carnes SA");
        });
    }
}
