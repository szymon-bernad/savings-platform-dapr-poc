using Carter;
using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Text.Json.Nodes;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;

namespace SavingsPlatform.Api.Api.Modules
{
    public class PlatformModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/v1/accounts/:handle-created-event",
              [Topic("pubsub", "accountcreated")] 
                async (AccountCreated evt, 
                IActorProxyFactory actorProxyFactory,
                IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory,
                IAggregateRootFactory<SettlementAccount, SettlementAccountState> settlementAccountFactory) =>
              {
                  if (evt.AccountType == AccountType.SavingsAccount && evt.TransferId != null)
                  {
                      var actorInstance = actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                      new ActorId(evt.TransferId),
                          nameof(DepositTransferActor));
                      var savingsAcc = await iasaFactory.GetInstanceByExternalRefAsync(evt.ExternalRef);
                      var settlementAcc = await settlementAccountFactory.GetInstanceByExternalRefAsync(savingsAcc.State!.SettlementAccountRef!);

                      await actorInstance.HandleStartAfterAccountCreation(savingsAcc.State!.Key, settlementAcc.State!.Key);
                  }
              });

            app.MapPost("v1/accounts/:handle-debited-event",
                        [Topic("pubsub", "accountdebited")] async (AccountDebited @event, IActorProxyFactory actorProxyFactory) =>
                        {
                            if (@event.TransferId != null)
                            {
                                var actorInstance = actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                                    new ActorId(@event.TransferId),
                                    nameof(DepositTransferActor));

                                await actorInstance.HandleDebitedEventAsync();
                            }
                        });

            app.MapPost("v1/accounts/:handle-credited-event",
                        [Topic("pubsub", "accountcredited")] async (AccountCredited @event, IActorProxyFactory actorProxyFactory) =>
                        {
                            if (@event.TransferId != null)
                            {
                                var actorInstance = actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                                    new ActorId(@event.TransferId),
                                    nameof(DepositTransferActor));

                                await actorInstance.HandleCreditedEventAsync();
                            }
                        });

            app.MapPost("v1/accounts/:handle-iasaactivated-event",
                        [Topic("pubsub", "instantaccesssavingsaccountactivated")] async (InstantAccessSavingsAccountActivated @event, IActorProxyFactory actorProxyFactory) =>
                        {
                            if (@event.AccountId != null)
                            {
                                var actorInstance = actorProxyFactory.CreateActorProxy<IInterestAccrualActor>(
                                    new ActorId(@event.AccountId),
                                    nameof(InterestAccrualActor));

                                await actorInstance.InitiateAsync(
                                    new InterestAccrualData
                                    {
                                        AccountKey = @event.AccountId,
                                        LastAccrualDate = null
                                    });
                            }
                        });

            app.MapPost("v1/accounts/:handle-interestaccrued-event",
                        [Topic("pubsub", "accountinterestaccrued")] async (AccountInterestAccrued @event, IActorProxyFactory actorProxyFactory) =>
                        {
                            if (@event.AccountId != null)
                            {
                                var actorInstance = actorProxyFactory.CreateActorProxy<IInterestAccrualActor>(
                                    new ActorId(@event.AccountId),
                                    nameof(InterestAccrualActor));

                                await actorInstance.HandleAccruedEventAsync(@event.Timestamp);
                            }
                        });

            app.MapPost("/v1/commands",
                        [Topic("pubsub", "commands")] async (CloudEvent<JsonObject> evt) =>
                        {
                            if (evt != null && evt.Data != null)
                            {
                                var typeProperty = evt.Data["EventType"]!.GetValue<string>();
                                var res = JsonDeserializeHelper.Deserialize(evt.Data, Type.GetType(typeProperty)!);
                            }

                            await Task.Delay(250);
                        });

            app.MapGet("/v1/platforms/:get-ids",
                        async (IStateEntryRepository<SettlementAccountState> repo) =>
                        {
                            var platformIds = (await repo.QueryAccountsByKeyAsync("data.type", string.Empty))
                                        .Select(acc => acc.PlatformId)
                                        .Distinct();
                            return Results.Ok(platformIds);
                        });
        }
    }
}
