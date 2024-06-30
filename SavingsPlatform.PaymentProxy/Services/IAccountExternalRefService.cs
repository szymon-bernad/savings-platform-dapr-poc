using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.PaymentProxy.Services
{
    public interface IAccountExternalRefService
    {
        public Task<AccountExternalMappingEntry> GetAccountEntryByExternalRef(string externalRef);

        public Task<PlatformMappingEntry> GetPlatformEntry(string platformId);

        public Task StoreAccountMapping(AccountExternalMappingEntry entry);

        public Task StorePlatformMapping(PlatformMappingEntry entry);
    }
}
