using System.Text.Json.Nodes;

namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record DepositRequest(
        string TransactionId,
        string ExternalRef,
        DepositRequestType Type,
        IDictionary<string, string>? Details);
}
