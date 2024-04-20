using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;

namespace SavingsPlatform.EventStore.Models
{
    public class SavingsPlatformOverview
    {
        public Guid Id { get; set; }

        public decimal SavingsTotalAmount {  get; set; } = decimal.Zero;

        public decimal DailyInflowUntilNow { get; set; } = decimal.Zero;

        public decimal DailyOutflowUntilNow { get; set; } = decimal.Zero;

        public DateTime? LatestInflowDate { get; set; }

        public DateTime? LatestOutflowDate { get; set; }

        public int SavingsAccountsCount { get; set; } = 0;

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
            }

            if (evt.AccountType == AccountType.SavingsAccount)
            {
                this.SavingsTotalAmount += evt.Amount;

                if (this.LatestInflowDate is null || this.LatestInflowDate.Value!.Date < evt.Timestamp.Date)
                {
                    this.DailyInflowUntilNow = evt.Amount;
                }
                else
                {
                    this.DailyInflowUntilNow += evt.Amount;
                }
                this.LatestInflowDate = evt.Timestamp;
            }
        }

        public void Apply(AccountDebited evt)
        {
            if (evt.AccountType == AccountType.SavingsAccount)
            {
                this.SavingsTotalAmount += evt.Amount;

                if (this.LatestOutflowDate is null || this.LatestOutflowDate.Value!.Date < evt.Timestamp.Date)
                {
                    this.DailyOutflowUntilNow = evt.Amount;
                }
                else
                {
                    this.DailyOutflowUntilNow += evt.Amount;
                }
                this.LatestOutflowDate = evt.Timestamp;
            }
        }
    }
}
