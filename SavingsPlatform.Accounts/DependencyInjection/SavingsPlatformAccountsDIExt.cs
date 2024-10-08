﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Services;

namespace SavingsPlatform.Accounts.DependencyInjection
{
    public static class SavingsPlatformAccountsDIExt
    {
        public static IServiceCollection AddSavingsAccounts(
            this IServiceCollection services,
            ConfigurationManager configuration,
            int daprPort)
        {
            services.AddOptions<SavingsAccountsStateStoreConfig>().Bind(configuration.GetSection("StateStore"));
            services.AddOptions<SimulationConfig>().Bind(configuration.GetSection("SimulationConfig"));
            services.AddScoped<IStateEntryRepository<SettlementAccountState>, SettlementAccountRepository>();
            services.AddTransient<IStateMapper<AggregateState<SettlementAccountDto>, SettlementAccountState>, SettlementAccountStateMapper>();
            services.AddTransient<ISettlementAccountFactory, SettlementAccountFactory>();

            services.AddTransient<IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountStateMapper>();
            services.AddScoped<IStateEntryRepository<InstantAccessSavingsAccountState>, InstantAccessSavingsAccountRepository>();
            services.AddTransient<IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountFactory>();
            services.AddTransient<IEventPublishingService, DaprEventPublishingService>();
            services.AddActors(options =>
            {
                options.Actors.RegisterActor<DepositTransferActor>();
                options.Actors.RegisterActor<InterestAccrualActor>();
            });
            return services;
        }
    }
}
