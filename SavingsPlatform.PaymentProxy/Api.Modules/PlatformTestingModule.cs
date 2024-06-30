using Carter;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.PaymentProxy.ApiClients;
using SavingsPlatform.PaymentProxy.Requests;
using SavingsPlatform.PaymentProxy.Services;
using System.Globalization;

namespace SavingsPlatform.PaymentProxy.Api.Modules
{
    public class PlatformTestingModule : ICarterModule
    {
  
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("tests/create-and-transfer",
                async (ISavingsPlatformApiClient savingsPlatformApiClient,
                       CreateAndTransferTestRequest testRequest) =>
            {
                var depositRequests = new List<DepositRequest>();
                var rnd = new Random();

                for (int q = 0; q < testRequest.NumberOfAccounts; ++q)
                {
                    var r = (decimal)Math.Round(1.0 + (0.2 * (rnd.NextDouble() - 0.5)), 3);
                    var interestRate = r * 5.0m;

                    var a = (decimal)(1.0 + (0.2 * (rnd.NextDouble() - 0.5)));
                    var amount = Math.Round(a * testRequest.avgAmount,2);
                    depositRequests.Add(
                        new DepositRequest(
                            $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}_{interestRate}_{(int)(amount*100)}",
                            DepositRequestType.CreateNew,
                            new Dictionary<string, string>
                            {
                                [DepositRequestDetailsKeys.InterestRate] = interestRate.ToString(CultureInfo.InvariantCulture),
                                [DepositRequestDetailsKeys.PlatformId] = testRequest.PlatformId,
                            }));
                    depositRequests.Add(
                        new DepositRequest(
                            $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}_{interestRate}_{(int)(amount * 100)}",
                            DepositRequestType.Transfer,
                            new Dictionary<string, string>
                            {
                                [DepositRequestDetailsKeys.TransferAmount] = amount.ToString(CultureInfo.InvariantCulture),
                                [DepositRequestDetailsKeys.TransferDirection] = $"{TransferDirection.ToSavingsAccount}",
                                [DepositRequestDetailsKeys.PlatformId] = testRequest.PlatformId,
                            })
                        );
                }

                await savingsPlatformApiClient.ProcessFile(new DepositRequestsFile("test-file", depositRequests));
            });
        }
    }
}
