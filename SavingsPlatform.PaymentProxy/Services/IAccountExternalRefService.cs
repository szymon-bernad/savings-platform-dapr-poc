using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.PaymentProxy.Services
{
    public interface IAccountExternalRefService
    {
        Task StoreAccountMapping(AccountExternalMappingEntry entry);

        Task<AccountExternalMappingEntry> GetEntryByExternalRef(string externalRef);
    }
}
