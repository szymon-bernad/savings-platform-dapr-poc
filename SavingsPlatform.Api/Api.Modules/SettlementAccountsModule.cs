using Carter;
using Dapr.Client;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.Api.Api.Modules
{
    public class SettlementAccountsModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/v1/settlement-account/{refid}", 
                async (string refid, 
                        IStateEntryRepository<SettlementAccountState> repo, 
                        ILogger<SettlementAccountsModule> logger) =>
            {

                logger.LogInformation("Received Query Request for settlement account with external ref: {refid}", refid);
                var result = await repo.QueryAccountsByKeyAsync(
                    new string[] { "data.externalRef", "data.type" },
                    new string[] { refid, $"{nameof(AccountType.SettlementAccount)}" } );
                return Results.Ok(result);
            });

            app.MapPost("/v1/settlement-account/:credit",
                async (CreditAccount request,
                        ISettlementAccountFactory aggregateFactory,
                        DaprClient daprClient) =>
                {
                    var instance = await aggregateFactory.GetInstanceAsync(request.AccountId);
                    await instance.CreditAsync(request);
                    
                    return Results.NoContent();
                });

            app.MapPost("v1/settlement-account/:debit",
                 async (DebitSettlementAccount request,
                        ISettlementAccountFactory aggregateFactory,
                        DaprClient daprClient) =>
                 {
                     var instance = await aggregateFactory.GetInstanceAsync(request.AccountId);
                     await instance.DebitAsync(request);

                     return Results.NoContent();
                 });
        }
    }
}
