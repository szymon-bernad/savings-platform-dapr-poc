using MediatR;

namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record DebitAccount(string AccountId, decimal Amount, DateTime TransactionDate, string? TransferId) : IRequest;
}
