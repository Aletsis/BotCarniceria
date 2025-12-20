using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;
using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Core.Domain.Enums;
using MediatR;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

/// <summary>
/// Handler for getting all invoice requests
/// </summary>
public class GetAllSolicitudesFacturaQueryHandler : IRequestHandler<GetAllSolicitudesFacturaQuery, List<SolicitudFacturaDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAllSolicitudesFacturaQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SolicitudFacturaDto>> Handle(GetAllSolicitudesFacturaQuery request, CancellationToken cancellationToken)
    {
        var solicitudes = await _unitOfWork.SolicitudesFactura.GetAllAsync();
        return solicitudes
            .OrderByDescending(s => s.FechaSolicitud)
            .Select(MapToDto)
            .ToList();
    }

    private static SolicitudFacturaDto MapToDto(SolicitudFactura solicitud)
    {
        var usoCfdiDesc = SatCatalogs.UsosCfdi.TryGetValue(solicitud.UsoCFDI, out var usoDesc) 
            ? usoDesc 
            : solicitud.UsoCFDI;

        var regimenDesc = SatCatalogs.RegimenesFiscales.TryGetValue(solicitud.DatosFacturacion.RegimenFiscal, out var regDesc)
            ? regDesc
            : solicitud.DatosFacturacion.RegimenFiscal;

        return new SolicitudFacturaDto
        {
            SolicitudFacturaID = solicitud.SolicitudFacturaID,
            ClienteID = solicitud.ClienteID,
            ClienteNombre = solicitud.Cliente.Nombre,
            ClienteTelefono = solicitud.Cliente.NumeroTelefono,
            Folio = solicitud.Folio,
            Total = solicitud.Total,
            UsoCFDI = solicitud.UsoCFDI,
            UsoCFDIDescripcion = usoCfdiDesc,
            RazonSocial = solicitud.DatosFacturacion.RazonSocial,
            RFC = solicitud.DatosFacturacion.RFC,
            Calle = solicitud.DatosFacturacion.Calle,
            Numero = solicitud.DatosFacturacion.Numero,
            Colonia = solicitud.DatosFacturacion.Colonia,
            CodigoPostal = solicitud.DatosFacturacion.CodigoPostal,
            Correo = solicitud.DatosFacturacion.Correo,
            RegimenFiscal = solicitud.DatosFacturacion.RegimenFiscal,
            RegimenFiscalDescripcion = regimenDesc,
            Estado = solicitud.Estado.ToString(),
            FechaSolicitud = solicitud.FechaSolicitud,
            FechaProcesada = solicitud.FechaProcesada,
            Notas = solicitud.Notas
        };
    }
}

/// <summary>
/// Handler for getting an invoice request by ID
/// </summary>
public class GetSolicitudFacturaByIdQueryHandler : IRequestHandler<GetSolicitudFacturaByIdQuery, SolicitudFacturaDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSolicitudFacturaByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<SolicitudFacturaDto?> Handle(GetSolicitudFacturaByIdQuery request, CancellationToken cancellationToken)
    {
        var solicitud = await _unitOfWork.SolicitudesFactura.GetByIdAsync(request.SolicitudFacturaID);
        if (solicitud == null) return null;

        var usoCfdiDesc = SatCatalogs.UsosCfdi.TryGetValue(solicitud.UsoCFDI, out var usoDesc)
            ? usoDesc
            : solicitud.UsoCFDI;

        var regimenDesc = SatCatalogs.RegimenesFiscales.TryGetValue(solicitud.DatosFacturacion.RegimenFiscal, out var regDesc)
            ? regDesc
            : solicitud.DatosFacturacion.RegimenFiscal;

        return new SolicitudFacturaDto
        {
            SolicitudFacturaID = solicitud.SolicitudFacturaID,
            ClienteID = solicitud.ClienteID,
            ClienteNombre = solicitud.Cliente.Nombre,
            ClienteTelefono = solicitud.Cliente.NumeroTelefono,
            Folio = solicitud.Folio,
            Total = solicitud.Total,
            UsoCFDI = solicitud.UsoCFDI,
            UsoCFDIDescripcion = usoCfdiDesc,
            RazonSocial = solicitud.DatosFacturacion.RazonSocial,
            RFC = solicitud.DatosFacturacion.RFC,
            Calle = solicitud.DatosFacturacion.Calle,
            Numero = solicitud.DatosFacturacion.Numero,
            Colonia = solicitud.DatosFacturacion.Colonia,
            CodigoPostal = solicitud.DatosFacturacion.CodigoPostal,
            Correo = solicitud.DatosFacturacion.Correo,
            RegimenFiscal = solicitud.DatosFacturacion.RegimenFiscal,
            RegimenFiscalDescripcion = regimenDesc,
            Estado = solicitud.Estado.ToString(),
            FechaSolicitud = solicitud.FechaSolicitud,
            FechaProcesada = solicitud.FechaProcesada,
            Notas = solicitud.Notas
        };
    }
}

/// <summary>
/// Handler for getting invoice requests by client
/// </summary>
public class GetSolicitudesFacturaByClienteQueryHandler : IRequestHandler<GetSolicitudesFacturaByClienteQuery, List<SolicitudFacturaDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetSolicitudesFacturaByClienteQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<SolicitudFacturaDto>> Handle(GetSolicitudesFacturaByClienteQuery request, CancellationToken cancellationToken)
    {
        var solicitudes = await _unitOfWork.SolicitudesFactura.GetByClienteIdAsync(request.ClienteID);
        return solicitudes.Select(s =>
        {
            var usoCfdiDesc = SatCatalogs.UsosCfdi.TryGetValue(s.UsoCFDI, out var usoDesc)
                ? usoDesc
                : s.UsoCFDI;

            var regimenDesc = SatCatalogs.RegimenesFiscales.TryGetValue(s.DatosFacturacion.RegimenFiscal, out var regDesc)
                ? regDesc
                : s.DatosFacturacion.RegimenFiscal;

            return new SolicitudFacturaDto
            {
                SolicitudFacturaID = s.SolicitudFacturaID,
                ClienteID = s.ClienteID,
                ClienteNombre = s.Cliente.Nombre,
                ClienteTelefono = s.Cliente.NumeroTelefono,
                Folio = s.Folio,
                Total = s.Total,
                UsoCFDI = s.UsoCFDI,
                UsoCFDIDescripcion = usoCfdiDesc,
                RazonSocial = s.DatosFacturacion.RazonSocial,
                RFC = s.DatosFacturacion.RFC,
                Calle = s.DatosFacturacion.Calle,
                Numero = s.DatosFacturacion.Numero,
                Colonia = s.DatosFacturacion.Colonia,
                CodigoPostal = s.DatosFacturacion.CodigoPostal,
                Correo = s.DatosFacturacion.Correo,
                RegimenFiscal = s.DatosFacturacion.RegimenFiscal,
                RegimenFiscalDescripcion = regimenDesc,
                Estado = s.Estado.ToString(),
                FechaSolicitud = s.FechaSolicitud,
                FechaProcesada = s.FechaProcesada,
                Notas = s.Notas
            };
        }).ToList();
    }
}

/// <summary>
/// Handler for creating a new invoice request
/// </summary>
public class CreateSolicitudFacturaCommandHandler : IRequestHandler<CreateSolicitudFacturaCommand, long>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateSolicitudFacturaCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<long> Handle(CreateSolicitudFacturaCommand request, CancellationToken cancellationToken)
    {
        // Get client to retrieve billing data
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(request.ClienteID);
        if (cliente == null)
            throw new InvalidOperationException($"Cliente con ID {request.ClienteID} no encontrado");

        if (cliente.DatosFacturacion == null)
            throw new InvalidOperationException("El cliente no tiene datos de facturación registrados");

        // Create the invoice request
        var solicitud = SolicitudFactura.Create(
            request.ClienteID,
            request.Folio,
            request.Total,
            request.UsoCFDI,
            cliente.DatosFacturacion,
            request.Notas
        );

        await _unitOfWork.SolicitudesFactura.AddAsync(solicitud);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return solicitud.SolicitudFacturaID;
    }
}

/// <summary>
/// Handler for updating invoice request status
/// </summary>
public class UpdateSolicitudFacturaEstadoCommandHandler : IRequestHandler<UpdateSolicitudFacturaEstadoCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSolicitudFacturaEstadoCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdateSolicitudFacturaEstadoCommand request, CancellationToken cancellationToken)
    {
        var solicitud = await _unitOfWork.SolicitudesFactura.GetByIdAsync(request.SolicitudFacturaID);
        if (solicitud == null)
            return false;

        if (!Enum.TryParse<EstadoSolicitudFactura>(request.NuevoEstado, out var nuevoEstado))
            throw new ArgumentException($"Estado inválido: {request.NuevoEstado}");

        solicitud.CambiarEstado(nuevoEstado);

        if (!string.IsNullOrWhiteSpace(request.Notas))
        {
            solicitud.ActualizarNotas(request.Notas);
        }

        await _unitOfWork.SolicitudesFactura.UpdateAsync(solicitud);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
