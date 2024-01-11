using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Aggregates.Settlement.Models
{
    public class SettlementAccountState : IAggregateStateEntry
    {
        public string Key { get; init; }
        public string? Etag { get; set; }
        public string? ExternalRef { get; init; }
        public DateTime? OpenedOn { get; set; }
        public decimal TotalBalance { get; set; }
        public Guid? LastTransactionId { get; set; }
        public bool HasUnpublishedEvents { get; set; } = false;
        public ICollection<object>? UnpublishedEvents { get; set; } = default;
        public AccountType Type = AccountType.SettlementAccount;
    }
}
