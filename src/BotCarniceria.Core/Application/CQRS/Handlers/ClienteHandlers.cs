using MediatR;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.CQRS.Commands;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

public class ClienteHandlers : 
    IRequestHandler<GetAllClientesQuery, List<ClienteDto>>,
    IRequestHandler<GetClienteByIdQuery, ClienteDto?>,
    IRequestHandler<UpdateClienteCommand, bool>,
    IRequestHandler<ToggleClienteActivoCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ClienteHandlers(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ClienteDto>> Handle(GetAllClientesQuery request, CancellationToken cancellationToken)
    {
        var clientes = await _unitOfWork.Clientes.GetAllAsync();
        // Implement searching if needed, for now return all
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
             clientes = clientes.Where(c => c.Nombre.Contains(request.SearchTerm, StringComparison.OrdinalIgnoreCase) || 
                                            c.NumeroTelefono.Contains(request.SearchTerm)).ToList();
        }
        return clientes.Select(ClienteDto.FromEntity).ToList();
    }

    public async Task<ClienteDto?> Handle(GetClienteByIdQuery request, CancellationToken cancellationToken)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(request.Id);
        return cliente != null ? ClienteDto.FromEntity(cliente) : null;
    }

    public async Task<bool> Handle(UpdateClienteCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(request.ClienteID);
        if (cliente == null) return false;

        cliente.ActualizarDatos(request.Nombre, request.Direccion);
        await _unitOfWork.Clientes.UpdateAsync(cliente);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }

    public async Task<bool> Handle(ToggleClienteActivoCommand request, CancellationToken cancellationToken)
    {
        var cliente = await _unitOfWork.Clientes.GetByIdAsync(request.ClienteID);
        if (cliente == null) return false;

        cliente.ToggleActivo(request.Activo);
        await _unitOfWork.Clientes.UpdateAsync(cliente);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }
}


