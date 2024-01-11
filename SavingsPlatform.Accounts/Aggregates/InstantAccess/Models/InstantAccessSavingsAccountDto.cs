using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess.Models
{
    public record InstantAccessSavingsAccountDto(
        string Key,
        string ExternalRef,
        string SettlementAccountRef,
        DateTime? OpenedOn,
        DateTime? ActivatedOn,
        decimal InterestRate,
        decimal TotalBalance,
        decimal AccruedInterest,
        Guid? LastTransactionId,
        ProcessFrequency InterestApplicationFrequency = ProcessFrequency.Weekly,
        DateTime? InterestApplicationDueOn = null,
        AccountType Type = AccountType.SavingsAccount);
}
