using Marten.Schema;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Aggregates.Settlement.Models
{
    public class SettlementAccountState : IAggregateStateEntry
    {
        public required string Key { get; init; }
        public string? Etag { get; set; }

        public int Version { get; set; } = 0;
        public string? ExternalRef { get; init; }
        public DateTime? OpenedOn { get; set; }
        public decimal TotalBalance { get; set; }
        public Guid? LastTransactionId { get; set; }
        public bool HasUnpublishedEvents { get; set; } = false;
        public ICollection<object>? UnpublishedEvents { get; set; } = default;
        public string PlatformId { get; init; } = string.Empty;

        public AccountType Type = AccountType.SettlementAccount;

        public Guid? VersionId { get; set; }
    }
}
