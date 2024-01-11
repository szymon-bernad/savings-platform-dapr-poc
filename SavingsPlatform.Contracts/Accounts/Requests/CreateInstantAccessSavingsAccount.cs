namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record CreateInstantAccessSavingsAccount(
        string ExternalRef,
        decimal InterestRate,
        string SettlementAccountRef);
}
