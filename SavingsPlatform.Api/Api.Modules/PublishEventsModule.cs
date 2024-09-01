using Carter;
using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Api.Api.Modules
{
    public class PublishEventsModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/publish-events", 
                async (IStateEntryRepository<SettlementAccountState> settlementRepository,
                       IStateEntryRepository<InstantAccessSavingsAccountState> iasaRepository,
                       IMediator mediator
                       ) =>
            {
                var settleRes = await settlementRepository.QueryAccountsByKeyAsync(new string[] { "hasUnpublishedEvents" }, new string[] { "true" }, false);

                await Task.WhenAll(
                    settleRes.Select(
                        acc => mediator.Send(new PublishEventsCommand(acc.Key, AccountType.SettlementAccount))
                        ));

                var iasaRes = await iasaRepository.QueryAccountsByKeyAsync(new string[] { "hasUnpublishedEvents" }, new string[] { "true" }, false);

                await Task.WhenAll(
                    iasaRes.Select(
                        acc => mediator.Send(new PublishEventsCommand(acc.Key, AccountType.SavingsAccount))
                        ));

                return Results.Ok();
            });

            app.MapMethods("/publish-events", new string[] { "OPTIONS" },
                (HttpContext ctx) => Task.FromResult(Results.Accepted()));
        }
    }
}
