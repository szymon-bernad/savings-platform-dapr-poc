using MediatR;

namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record DebitSettlementAccount(string AccountId, decimal Amount, DateTime TransactionDate, string? TransferId) : IRequest;
}
