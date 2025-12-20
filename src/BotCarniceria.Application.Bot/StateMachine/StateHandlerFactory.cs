using BotCarniceria.Application.Bot.Interfaces;
using BotCarniceria.Application.Bot.StateMachine.Handlers;
using BotCarniceria.Core.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace BotCarniceria.Application.Bot.StateMachine;

public class StateHandlerFactory : IStateHandlerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public StateHandlerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IConversationStateHandler GetHandler(ConversationState state)
    {
        return state switch
        {
            ConversationState.START => _serviceProvider.GetRequiredService<StartStateHandler>(),
            ConversationState.MENU => _serviceProvider.GetRequiredService<MenuStateHandler>(),
            ConversationState.ASK_NAME => _serviceProvider.GetRequiredService<AskNameStateHandler>(),
            ConversationState.ASK_ADDRESS => _serviceProvider.GetRequiredService<AskAddressStateHandler>(),
            ConversationState.TAKING_ORDER => _serviceProvider.GetRequiredService<TakingOrderStateHandler>(),
            ConversationState.ADDING_MORE => _serviceProvider.GetRequiredService<AddingMoreStateHandler>(),
            ConversationState.AWAITING_CONFIRM => _serviceProvider.GetRequiredService<AwaitingConfirmStateHandler>(),
            ConversationState.CONFIRM_ADDRESS => _serviceProvider.GetRequiredService<ConfirmAddressStateHandler>(),
            ConversationState.SELECT_PAYMENT => _serviceProvider.GetRequiredService<SelectPaymentStateHandler>(),
            ConversationState.CONFIRM_LATE_ORDER => _serviceProvider.GetRequiredService<ConfirmLateOrderStateHandler>(),
            
            // Billing States
            ConversationState.BILLING_WARNING => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_CHECK_DATA => _serviceProvider.GetRequiredService<BillingStateHandler>(), // Though logic likely in Menu, handler exists just in case
            ConversationState.BILLING_ASK_RAZON_SOCIAL => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_RFC => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_CALLE => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_NUMERO => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_COLONIA => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_CP => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_CORREO => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_REGIMEN => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_CONFIRM_DATA => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_NOTE_FOLIO => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_NOTE_TOTAL => _serviceProvider.GetRequiredService<BillingStateHandler>(),
            ConversationState.BILLING_ASK_CFDI => _serviceProvider.GetRequiredService<BillingStateHandler>(),

            _ => _serviceProvider.GetRequiredService<MenuStateHandler>() // Default
        };
    }
}
