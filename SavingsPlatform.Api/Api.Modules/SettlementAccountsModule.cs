using Carter;
using Dapr.Client;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.Api.Api.Modules
{
    public class SettlementAccountsModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/v1/settlement-account/{refid}", async (string refid, IStateEntryRepository<SettlementAccountState> repo) =>
            {
                var result = await repo.QueryAccountsByKeyAsync("data.externalRef", refid);
                return Results.Ok(result);
            });

            app.MapPost("/v1/settlement-account/:credit",
                async (CreditAccount request,
                        IAggregateRootFactory<SettlementAccount, SettlementAccountState> aggregateFactory,
                        DaprClient daprClient) =>
                {
                    var instance = await aggregateFactory.GetInstanceAsync(request.AccountId);
                    await instance.CreditAsync(request);

                    return Results.NoContent();
                });

            app.MapPost("v1/settlement-account/:debit",
                 async (DebitAccount request,
                        IAggregateRootFactory<SettlementAccount, SettlementAccountState> aggregateFactory,
                        DaprClient daprClient) =>
                 {
                     var instance = await aggregateFactory.GetInstanceAsync(request.AccountId);
                     await instance.DebitAsync(request);

                     return Results.NoContent();
                 });
        }
    }
}
