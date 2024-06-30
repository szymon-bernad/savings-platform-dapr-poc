using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.PaymentProxy.ApiClients
{
    public interface ISavingsPlatformApiClient
    {
        public Task ProcessFile(DepositRequestsFile request);
    }
}
