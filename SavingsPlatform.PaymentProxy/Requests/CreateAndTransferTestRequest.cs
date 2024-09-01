namespace SavingsPlatform.PaymentProxy.Requests
{
    public record CreateAndTransferTestRequest(string PlatformId, int NumberOfAccounts, decimal avgAmount);
}
