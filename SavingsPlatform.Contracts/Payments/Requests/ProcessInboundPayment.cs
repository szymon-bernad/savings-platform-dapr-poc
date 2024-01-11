namespace SavingsPlatform.Contracts.Payments.Requests
{
    public record ProcessInboundPayment(
      string AccountRef,
      decimal Amount,
      DateTime TransactionDate,
      string Reference);
}
