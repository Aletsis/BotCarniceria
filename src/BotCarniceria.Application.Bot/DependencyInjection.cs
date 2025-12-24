using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Core.Application.Interfaces;
using BotCarniceria.Application.Bot.Services;
using BotCarniceria.Application.Bot.StateMachine;
using BotCarniceria.Application.Bot.StateMachine.Handlers;
using Microsoft.Extensions.DependencyInjection;

namespace BotCarniceria.Application.Bot;

public static class DependencyInjection
{
    public static IServiceCollection AddBotApplication(this IServiceCollection services)
    {
        // Core Services
        services.AddScoped<IIncomingMessageHandler, IncomingMessageHandler>();
        services.AddScoped<IStateHandlerFactory, StateHandlerFactory>();

        // State Handlers
        services.AddScoped<StartStateHandler>();
        services.AddScoped<MenuStateHandler>();
        services.AddScoped<AskNameStateHandler>();
        services.AddScoped<AskAddressStateHandler>();
        services.AddScoped<TakingOrderStateHandler>();
        services.AddScoped<AddingMoreStateHandler>();
        services.AddScoped<AwaitingConfirmStateHandler>();
        services.AddScoped<ConfirmAddressStateHandler>();
        services.AddScoped<SelectPaymentStateHandler>();
        services.AddScoped<ConfirmLateOrderStateHandler>();
        services.AddScoped<BillingStateHandler>();

        return services;
    }
}
