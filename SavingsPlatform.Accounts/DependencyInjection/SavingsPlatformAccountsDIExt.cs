using Microsoft.Extensions.Configuration;
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
            services.AddHttpClient<IStateEntryRepository<SettlementAccountState>, 
                SettlementAccountRepository>(
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri($"http://localhost:{daprPort}");
                });
            services.AddScoped<IStateMapper<AggregateState<SettlementAccountDto>, SettlementAccountState>, SettlementAccountStateMapper>();
            services.AddScoped<IAggregateRootFactory<SettlementAccount, SettlementAccountState>, SettlementAccountFactory>();

            services.AddScoped<IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountStateMapper>();
            services.AddHttpClient<IStateEntryRepository<InstantAccessSavingsAccountState>, InstantAccessSavingsAccountRepository>(
                httpClient =>
                {
                    httpClient.BaseAddress = new Uri($"http://localhost:{daprPort}");
                });
            services.AddScoped<IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState>, InstantAccessSavingsAccountFactory>();
            services.AddScoped<IEventPublishingService, DaprEventPublishingService>();
            services.AddActors(options =>
            {
                options.Actors.RegisterActor<DepositTransferActor>();
                options.Actors.RegisterActor<InterestAccrualActor>();
            });
            return services;
        }
    }
}
