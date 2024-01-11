using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;

namespace SavingsPlatform.EventStore.Models
{
    public class SavingsPlatformOverview
    {
        public Guid Id { get; set; }

        public int TransfersTotalCount { get; set; } = 0;

        public decimal SavingsTotalAmount {  get; set; } = decimal.Zero;

        public int SavingsAccountsCount { get; set; } = 0;

        public int TransfersPendingCount { get; set; } = 0;

        public HashSet<string> Transfers {  get; set; } = new HashSet<string>();

        public void Apply(AccountCreated evt)
        {
            if (evt.AccountType == AccountType.SavingsAccount)
            {
                this.SavingsAccountsCount++;
            }
        }

        public void Apply(AccountCredited evt)
        {
            if (!string.IsNullOrEmpty(evt.TransferId) && !Transfers.Contains(evt.TransferId))
            {
                this.Transfers.Add(evt.TransferId);
                this.TransfersTotalCount++;
                this.SavingsTotalAmount += evt.Amount;
                this.TransfersPendingCount--;
            }
        }

        public void Apply(AccountDebited evt)
        {
            if (!string.IsNullOrEmpty(evt.TransferId) && !Transfers.Contains(evt.TransferId))
            {
                this.TransfersPendingCount++;
            }
        }
    }
}
