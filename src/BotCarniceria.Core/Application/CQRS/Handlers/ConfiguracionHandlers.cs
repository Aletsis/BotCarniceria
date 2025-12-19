using MediatR;
using BotCarniceria.Core.Application.DTOs;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.CQRS.Queries;
using BotCarniceria.Core.Application.CQRS.Commands;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

public class ConfiguracionHandlers : 
    IRequestHandler<GetAllConfiguracionesQuery, List<ConfiguracionDto>>,
    IRequestHandler<GetConfiguracionByKeyQuery, ConfiguracionDto?>,
    IRequestHandler<UpdateConfiguracionCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ConfiguracionHandlers(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ConfiguracionDto>> Handle(GetAllConfiguracionesQuery request, CancellationToken cancellationToken)
    {
        var configs = await _unitOfWork.Configuraciones.GetAllAsync();
        return configs.Select(ConfiguracionDto.FromEntity).ToList();
    }

    public async Task<ConfiguracionDto?> Handle(GetConfiguracionByKeyQuery request, CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Configuraciones.GetByClaveAsync(request.Key);
        return config != null ? ConfiguracionDto.FromEntity(config) : null;
    }

    public async Task<bool> Handle(UpdateConfiguracionCommand request, CancellationToken cancellationToken)
    {
        var config = await _unitOfWork.Configuraciones.GetByClaveAsync(request.Key);
        if (config == null) return false;

        config.ActualizarValor(request.Value);
        await _unitOfWork.Configuraciones.UpdateAsync(config);
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }
}
