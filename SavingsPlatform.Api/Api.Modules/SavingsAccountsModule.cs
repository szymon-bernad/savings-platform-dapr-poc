using Carter;
using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.Api.Api.Modules
{
    public class SavingsAccountsModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapGet("/v1/savings-account/{refid}", async (string refid, IStateEntryRepository<InstantAccessSavingsAccountState> repo) =>
            {
                var result = await repo.QueryAccountsByKeyAsync("data.externalRef", refid);
                return Results.Ok(result);
            });

            app.MapPost("/v1/settlement-accounts",
                async (IAggregateRootFactory<SettlementAccount, SettlementAccountState> aggregateFactory,
                       CreateSettlementAccount request) =>
            {
                var instance = await aggregateFactory.GetInstanceAsync();
                await instance.CreateAsync(request);

                return Results.Accepted();
            });

            app.MapPost("/v1/savings-accounts/:process",
                async (IMediator mediator, DepositRequest request) =>
                {
                    if(request.Details is null)
                    {
                        throw new ArgumentNullException(nameof(request.Details));
                    }

                    var processTask = request.Type switch
                    {
                        DepositRequestType.Transfer => ProcessTransfer(mediator, request),
                        DepositRequestType.CreateNew => ProcessCreateNew(mediator, request),
                        _ => Task.FromResult(Results.BadRequest()),
                    };
                    
                    await processTask;
                });
        }

        private static async Task<IResult> ProcessTransfer(IMediator mediator, DepositRequest request)
        {
            var transferCmd = JsonDeserializeHelper.Deserialize(
                           request.Details!,
                           typeof(TransferDepositCommand));

            if (transferCmd is not null)
            {
                await mediator.Send(transferCmd);
                return Results.Accepted();
            }

            return Results.BadRequest();
        }

        private static async Task<IResult> ProcessCreateNew(IMediator mediator, DepositRequest request)
        {
            var createCmd = JsonDeserializeHelper.Deserialize(
               request.Details!,
               typeof(CreateInstantSavingsAccountCommand));

            if (createCmd is not null)
            {
                await mediator.Send(createCmd);
                return Results.Accepted();
            }

            return Results.BadRequest();
        }
    }
}
