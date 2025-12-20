using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs;
using BotCarniceria.Core.Application.Interfaces.BackgroundJobs.Jobs;
using BotCarniceria.Core.Domain.Services;
using BotCarniceria.Infrastructure.BackgroundJobs;
using BotCarniceria.Infrastructure.BackgroundJobs.Configuration;
using BotCarniceria.Infrastructure.BackgroundJobs.Handlers;
using BotCarniceria.Infrastructure.BackgroundJobs.Services;
using BotCarniceria.Infrastructure.Persistence.Context;
using BotCarniceria.Infrastructure.Persistence;
using BotCarniceria.Infrastructure.Persistence.Repositories;
using BotCarniceria.Infrastructure.Resilience;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotCarniceria.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register CodePagesEncodingProvider to enable support for legacy code pages (e.g., CP850 used in thermal printers)
        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

        services.AddDbContext<BotCarniceriaDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly(typeof(BotCarniceriaDbContext).Assembly.FullName)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<ISessionRepository, SessionRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IConfiguracionRepository, ConfiguracionRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();
        services.AddScoped<ISolicitudFacturaRepository, SolicitudFacturaRepository>();
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();


        // Caching
        services.AddMemoryCache();
        services.AddScoped<ICacheService, Services.Caching.MemoryCacheService>();

        // WhatsApp Service with Circuit Breaker (Decorator Pattern)
        services.AddHttpClient();
        
        // Configure Circuit Breaker options
        services.Configure<WhatsAppCircuitBreakerOptions>(
            configuration.GetSection(WhatsAppCircuitBreakerOptions.SectionName));
        
        // Register metrics collector as singleton (shared across all requests)
        // Clean Architecture: Depende de la interfaz IResilienceMetricsCollector (Application)
        // y se implementa con ResilienceMetricsCollector (Infrastructure)
        services.AddSingleton<IResilienceMetricsCollector, Infrastructure.Metrics.ResilienceMetricsCollector>();
        
        // Register the inner WhatsApp service
        services.AddScoped<Services.External.WhatsApp.WhatsAppService>();
        
        // Register the decorator with Circuit Breaker
        services.AddScoped<IWhatsAppService>(provider =>
        {
            var innerService = provider.GetRequiredService<Services.External.WhatsApp.WhatsAppService>();
            var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<WhatsAppServiceWithCircuitBreaker>>();
            var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<WhatsAppCircuitBreakerOptions>>();
            var metricsCollector = provider.GetRequiredService<IResilienceMetricsCollector>();
            
            return new WhatsAppServiceWithCircuitBreaker(innerService, logger, options, metricsCollector);
        });

        services.AddScoped<IPrintingService, Services.External.Printing.PrintingService>();
        services.AddScoped<IPasswordHasher, Services.PasswordHasher>();
        services.AddScoped<IDateTimeProvider, Services.DateTimeProvider>();

        // Background Jobs with Hangfire
        services.AddBackgroundJobs(configuration);

        return services;
    }
}
