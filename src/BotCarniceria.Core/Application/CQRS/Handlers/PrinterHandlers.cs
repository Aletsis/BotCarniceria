using BotCarniceria.Core.Application.CQRS.Commands;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Domain.Constants;
using MediatR;
using System.Text.Json;

namespace BotCarniceria.Core.Application.CQRS.Handlers;

public class UpdatePrinterSettingsCommandHandler : IRequestHandler<UpdatePrinterSettingsCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public UpdatePrinterSettingsCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(UpdatePrinterSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = request.Settings;

        // Validation Logic (Business Rules)
        
        // Rule: Printer names must be unique (Case insensitive)
        var names = settings.Printers.Select(p => p.Name.ToLower()).ToList();
        if (names.Count != names.Distinct().Count())
        {
            // In a real scenario, we might want to return a Result<T> with error messages.
            // For now, returning false indicates failure. 
            // Ideally we throw a domain exception or return a result object.
            return false;
        }

        // Rule: Verify IP addresses are valid (Optional, but good practice)
        // ...

        // Rule: Ensure Default Printer is valid
        if (settings.Printers.Any() && !settings.Printers.Any(p => p.Name == settings.DefaultPrinterName))
        {
            // If default is invalid, set to first available
            settings.DefaultPrinterName = settings.Printers.First().Name;
        }
        else if (!settings.Printers.Any())
        {
            settings.DefaultPrinterName = string.Empty;
        }

        var json = JsonSerializer.Serialize(settings);
        
        var config = await _unitOfWork.Settings.GetByClaveAsync(ConfigurationKeys.Printers.Configuration);
        
        if (config == null)
        {
            // Create if not exists
            config = BotCarniceria.Core.Domain.Entities.Configuracion.Create(
                ConfigurationKeys.Printers.Configuration, 
                json, 
                BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Json, 
                "Configuracion de Impresoras"
            );
            await _unitOfWork.Settings.AddAsync(config);
        }
        else
        {
            config.ActualizarValor(json);
            await _unitOfWork.Settings.UpdateAsync(config);
        }
        
        return await _unitOfWork.SaveChangesAsync(cancellationToken) > 0;
    }
}
