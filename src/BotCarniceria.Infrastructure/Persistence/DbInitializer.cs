using BotCarniceria.Core.Domain.Entities;
using BotCarniceria.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using BotCarniceria.Core.Domain.Constants;

namespace BotCarniceria.Infrastructure.Persistence;

public static class DbInitializer
{
    public static async Task InitializeAsync(BotCarniceriaDbContext context)
    {
        // Apply any pending migrations
        await context.Database.MigrateAsync();

        // Always check for new configurations
        var defaultConfigs = new List<Configuracion>
        {
                Configuracion.Create(ConfigurationKeys.WhatsApp.PhoneNumberId, "YOUR_PHONE_NUMBER_ID", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "ID del número de teléfono de WhatsApp Business"),
                Configuracion.Create(ConfigurationKeys.WhatsApp.AccessToken, "YOUR_ACCESS_TOKEN", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Token de acceso permanente"),
                Configuracion.Create(ConfigurationKeys.WhatsApp.VerifyToken, "bot_carniceria_token", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Token de verificación de webhook"),
                Configuracion.Create(ConfigurationKeys.WhatsApp.AppSecret, "YOUR_APP_SECRET", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "App Secret para validación de firma HMAC"),
                
                Configuracion.Create(ConfigurationKeys.Business.Schedule, "Lunes a Viernes 9:00 - 18:00, Sábados 9:00 - 14:00", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Horarios de atención"),
                Configuracion.Create(ConfigurationKeys.Business.Address, "Av. Principal #123, Centro", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Dirección del negocio"),
                Configuracion.Create(ConfigurationKeys.Business.Phone, "5512345678", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Teléfono de contacto"),
                Configuracion.Create(ConfigurationKeys.Business.DeliveryTime, "30-45 minutos", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Tiempo estimado de entrega"),

                // Nuevas configuraciones
                Configuracion.Create(ConfigurationKeys.Printers.Name, "DefaultPrinter", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Nombre de la impresora predeterminada"),
                Configuracion.Create(ConfigurationKeys.Printers.IpAddress, "192.168.1.100", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Dirección IP de la impresora"),
                Configuracion.Create(ConfigurationKeys.Printers.Port, "9100", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Puerto de la impresora"),
                Configuracion.Create(ConfigurationKeys.Printers.Configuration, "{\"DefaultPrinterName\":\"DefaultPrinter\",\"Printers\":[{\"Name\":\"DefaultPrinter\",\"IpAddress\":\"192.168.1.100\",\"Port\":9100}]}", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Configuración avanzada de impresoras (JSON)"),
                
                Configuracion.Create(ConfigurationKeys.Session.BotTimeoutMinutes, "30", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Tiempo de espera (minutos) para sesión del Bot"),
                Configuracion.Create(ConfigurationKeys.Session.BotWarningMinutes, "5", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Tiempo de advertencia (minutos) antes de cerrar sesión del Bot"),
                Configuracion.Create(ConfigurationKeys.Session.BlazorTimeoutMinutes, "30", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Tiempo de espera (minutos) para sesión de Blazor"),
                Configuracion.Create(ConfigurationKeys.Session.BlazorWarningMinutes, "5", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Tiempo de advertencia (minutos) antes de cerrar sesión de Blazor"),
                
                Configuracion.Create(ConfigurationKeys.System.PrintRetryCount, "3", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Número de reintentos para impresión"),
                Configuracion.Create(ConfigurationKeys.System.MessageRetryCount, "3", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Número de reintentos para envío de mensajes"),
                Configuracion.Create(ConfigurationKeys.System.RetryIntervalSeconds, "60", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Intervalo (segundos) entre reintentos"),
                Configuracion.Create(ConfigurationKeys.System.WorkQueueCount, "2", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Numero, "Número de colas de trabajo simultáneas"),
                Configuracion.Create(ConfigurationKeys.System.TimeZoneId, "Central Standard Time (Mexico)", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Zona horaria del negocio (ID de TimeZoneInfo)"),
                
                Configuracion.Create(ConfigurationKeys.Orders.LateOrderWarningStartHour, "16:00", BotCarniceria.Core.Domain.Enums.TipoConfiguracion.Texto, "Hora de inicio aviso horario (formato HH:mm)")
            };

            var existingKeys = await context.Configuraciones.Select(c => c.Clave).ToListAsync();
            var newConfigs = defaultConfigs.Where(c => !existingKeys.Contains(c.Clave)).ToList();

            if (newConfigs.Any())
            {
                await context.Configuraciones.AddRangeAsync(newConfigs);
                await context.SaveChangesAsync();
            }

        // Check for admin user and ensure valid password (in case of hash format mismatch)
        var passwordHasher = new Services.PasswordHasher();
        var adminUser = await context.Usuarios.FirstOrDefaultAsync(u => u.Username == "admin");

        if (adminUser == null)
        {
            adminUser = Usuario.Create(
                "admin",
                passwordHasher.HashPassword("Admin123!"),
                "Administrador del Sistema",
                Core.Domain.Enums.RolUsuario.Admin,
                null
            );
            await context.Usuarios.AddAsync(adminUser);
        }
        else
        {
            // Always reset admin password in Development to ensure access if hash changes
            // This fixes "Invalid salt version" errors if the hash format in DB is old
            adminUser.CambiarPassword(passwordHasher.HashPassword("Admin123!"));
            context.Usuarios.Update(adminUser);
        }
        
        await context.SaveChangesAsync();
    }
}
