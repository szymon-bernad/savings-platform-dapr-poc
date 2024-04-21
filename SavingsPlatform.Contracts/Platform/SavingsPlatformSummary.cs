namespace SavingsPlatform.Contracts.Platform
{
    public record SavingsPlatformSummary
    {
        public Guid Id { get; init; }

        public decimal SavingsTotalAmount { get; init; } = decimal.Zero;

        public decimal DailyInflowUntilNow { get; init; } = decimal.Zero;

        public decimal DailyOutflowUntilNow { get; init; } = decimal.Zero;

        public DateTime? LatestInflowDate { get; init; }

        public DateTime? LatestOutflowDate { get; init; }

        public int SavingsAccountsCount { get; init; } = 0;
    }
}
