using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Interfaces;

namespace SavingsPlatform.Contracts.Accounts.Events
{
    public class AccountCreated : IEvent
    {
        public string? Id { get; set; }
        public string? ExternalRef { get; set; }
        public string? SettlementAccountRef { get; set; }
        public string? AccountId { get; set; }
        public AccountType AccountType { get; set; }
        public DateTime Timestamp { get; set; }
        public string? EventType { get; set; }
        public string? TransferId { get; set; }
    }
}
