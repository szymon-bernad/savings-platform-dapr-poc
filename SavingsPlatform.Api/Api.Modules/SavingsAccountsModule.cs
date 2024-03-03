using Carter;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Requests;
using static Google.Rpc.Context.AttributeContext.Types;

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

            app.MapPost("v1/savings-accounts/:process-file",
                async (IMediator mediator, DepositRequestsFile request) =>
                {
                    if (request?.Requests?.Any() ?? false)
                    {
                        var requestsGrouped = request.Requests.GroupBy(r => r.ExternalRef);

                        await Task.WhenAll(requestsGrouped.Select(g => this.ProcessRequestGroup(mediator, g.ToList())));
                    }
                });
        }

        private async Task ProcessRequestGroup(IMediator mediator, ICollection<DepositRequest> requestGroup)
        {
            if (!(requestGroup?.Any() ?? false))
            {
                throw new InvalidOperationException("No requests found in the request group.");
            }

            var firstReq = requestGroup.First(); 
            var createNewReqs = requestGroup.Where(r => r.Type == DepositRequestType.CreateNew);
            var transferReqs = requestGroup.Where(r => r.Type == DepositRequestType.Transfer);

            if (createNewReqs.Count() > 1)
            {
                throw new InvalidOperationException($"Cannot process more than 1 request of Type = {DepositRequestType.CreateNew} " +
                    $"for ExternalRef = {firstReq.ExternalRef}.");
            }

            if (transferReqs.Count() > 1)
            {
                throw new InvalidOperationException($"Cannot process more than 1 request of Type = {DepositRequestType.Transfer} " +
                    $"for ExternalRef = {firstReq.ExternalRef}.");
            }

            var transferId = transferReqs.Any() ? Guid.NewGuid().ToString() : null;
            var createReq = createNewReqs.FirstOrDefault();
            if (createReq is not null)
            {
                await ProcessCreateRequest(mediator, createReq, transferId);
            }

            var transferReq = transferReqs.FirstOrDefault();
            if (transferReq is not null)
            {
                await ProcessTransferRequest(mediator, transferReq, transferId, (createReq is not null));
            }
        }

        private async Task ProcessTransferRequest(IMediator mediator, DepositRequest transferReq, string? transferId, bool waitForAccountCreation = false)
        {
            var amount = 0m;
            transferReq.Details!.TryGetValue(DepositRequestDetailsKeys.TransferAmount, out var amountStr);
            if (amountStr is null)
            {
                throw new InvalidOperationException($"TransferAmount is required for Transfer request for ExternalRef = {transferReq.ExternalRef}.");
            }
            else if (!decimal.TryParse(amountStr, out amount))
            {
                throw new InvalidOperationException($"TransferAmount must be a valid decimal for Transfer request for ExternalRef = {transferReq.ExternalRef}.");
            }

            var direction = TransferDirection.ToSavingsAccount;
            transferReq.Details!.TryGetValue(DepositRequestDetailsKeys.TransferDirection, out var directionStr);
            if (directionStr is null)
            {
                throw new InvalidOperationException($"TransferDirection is required for Transfer request for ExternalRef = {transferReq.ExternalRef}.");
            }
            else if (!Enum.TryParse<TransferDirection>(directionStr, out direction))
            {
                throw new InvalidOperationException($"TransferDirection must be a valid TransferDirection for Transfer request for ExternalRef = {transferReq.ExternalRef}.");
            }

            var transferCmd = new TransferDepositCommand(
                transferReq.ExternalRef,
                DateTime.UtcNow,
                amount,
                direction,
                transferId,
                waitForAccountCreation);
            await mediator.Send(transferCmd);
        }

        private async Task ProcessCreateRequest(IMediator mediator, DepositRequest request, string? transferId)
        {
            request.Details!.TryGetValue(DepositRequestDetailsKeys.SettlementAccountRef, out var settlementAccRef);
            if (settlementAccRef is null)
            {
                throw new InvalidOperationException($"SettlementAccountRef is required for CreateNew request for ExternalRef = {request.ExternalRef}.");
            }

            var interestRate = 0m;
            request.Details!.TryGetValue(DepositRequestDetailsKeys.InterestRate, out var interestRateStr);
            if (interestRateStr is null)
            {
                throw new InvalidOperationException($"InterestRate is required for CreateNew request for ExternalRef = {request.ExternalRef}.");
            }
            else if (!decimal.TryParse(interestRateStr, out interestRate))
            {
                throw new InvalidOperationException($"InterestRate must be a valid decimal for CreateNew request for ExternalRef = {request.ExternalRef}.");
            }

            var createCmd = new CreateInstantSavingsAccountCommand(
                request.ExternalRef,
                interestRate,
                settlementAccRef,
                transferId);

            await mediator.Send(createCmd);
        }
    }
}
