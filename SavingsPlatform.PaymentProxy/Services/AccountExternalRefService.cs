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
            StateStoreName = stateStoreCfg?.Value?.StateStoreName ?? throw new ArgumentNullException(nameof(StateStoreName));
            PubSubName = stateStoreCfg?.Value?.PubSubName ?? throw new ArgumentNullException(nameof(PubSubName));
        }

        public Task<AccountExternalMappingEntry> GetEntryByExternalRef(string externalRef)
        {
            return _daprClient.GetStateAsync<AccountExternalMappingEntry>(
                StateStoreName,
                externalRef);
        }

        public async Task StoreAccountMapping(AccountExternalMappingEntry entry)
        {
            var res = await GetEntryByExternalRef(entry.ExternalRef);
            if (res != null)
            {
                throw new InvalidOperationException($"{nameof(AccountExternalMappingEntry)}" +
                    $" with externalRef = {entry.ExternalRef} already exists.");
            }

            await _daprClient.SaveStateAsync(StateStoreName, entry.ExternalRef, entry);
        }
    }
}
