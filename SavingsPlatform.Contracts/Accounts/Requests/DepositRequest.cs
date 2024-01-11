using System.Text.Json.Nodes;

namespace SavingsPlatform.Contracts.Accounts.Requests
{
    public record DepositRequest(
        string TransactionId,
        DepositRequestType Type,
        JsonObject? Details);
}
