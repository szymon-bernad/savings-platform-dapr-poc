namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record DepositRequestsFile(string FileName, ICollection<DepositRequest> Requests);
}
