namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record CreditAccount(string AccountId, decimal Amount, DateTime TransactionDate, string? TransferId);
}
