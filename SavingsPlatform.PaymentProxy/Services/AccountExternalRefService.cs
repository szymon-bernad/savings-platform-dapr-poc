using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Runtime.CompilerServices;

namespace SavingsPlatform.PaymentProxy.Services
{
    public class AccountExternalRefService : IAccountExternalRefService
    {
        private readonly DaprClient _daprClient;
        protected readonly string StateStoreName;
        protected readonly string PubSubName;

        public AccountExternalRefService(
            DaprClient daprClient,
            IOptions<SavingsAccountsStateStoreConfig> stateStoreCfg)
        {
            _daprClient = daprClient;
            StateStoreName = stateStoreCfg?.Value?.StateStoreName ?? throw new ArgumentNullException(nameof(stateStoreCfg));
            PubSubName = stateStoreCfg?.Value?.PubSubName ?? throw new ArgumentNullException(nameof(stateStoreCfg));
        }

        public Task<AccountExternalMappingEntry> GetAccountEntryByExternalRef(string externalRef)
        {
            return _daprClient.GetStateAsync<AccountExternalMappingEntry>(
                StateStoreName,
                externalRef);
        }

        public Task<PlatformMappingEntry> GetPlatformEntry(string platformId)
        {
            return _daprClient.GetStateAsync<PlatformMappingEntry>(
                StateStoreName,
                platformId);
        }

        public async Task StoreAccountMapping(AccountExternalMappingEntry entry)
        {
            var res = await GetAccountEntryByExternalRef(entry.ExternalRef);
            if (res != null)
            {
                throw new InvalidOperationException($"{nameof(AccountExternalMappingEntry)}" +
                    $" with externalRef = {entry.ExternalRef} already exists.");
            }

            await _daprClient.SaveStateAsync(StateStoreName, entry.ExternalRef, entry);
        }

        public async Task StorePlatformMapping(PlatformMappingEntry entry)
        {
            var res = await GetPlatformEntry(entry.PlatformId);
            if (res != null && !res.SettlementRef.Equals(entry.SettlementRef, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Found {nameof(PlatformMappingEntry)}" +
                    $" with Id = {entry.PlatformId} " +
                    $" and different {nameof(PlatformMappingEntry.SettlementRef)} value.");
            }

            await _daprClient.SaveStateAsync(StateStoreName, entry.PlatformId, entry);
        }
    }
}
