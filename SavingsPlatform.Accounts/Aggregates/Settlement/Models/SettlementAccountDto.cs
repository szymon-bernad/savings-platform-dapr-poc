using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Aggregates.Settlement.Models
{
    public record SettlementAccountDto(
        string Key,
        string ExternalRef,
        DateTime? OpenedOn,
        decimal TotalBalance,
        Guid? LastTransactionId,
        string? PlatformId,
        AccountType Type = AccountType.SettlementAccount);
}
