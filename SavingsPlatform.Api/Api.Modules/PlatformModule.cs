using Carter;
using Dapr;
using Dapr.Actors;
using Dapr.Actors.Client;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Api.Api.Modules
{
    public class PlatformModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
         //   app.MapPost("/v1/accounts/:handle-created-event",
         //     [Topic("pubsub", "AccountCreated")] async (AccountCreated evt, IAccountExternalRefService svc) =>
         //     {
         //         if (evt != null)
         //         {
         //             await svc.StoreAccountMapping(
         //             new AccountExternalMappingEntry(evt.ExternalRef!, evt.AccountId!, evt.AccountType));
         //         }
         //     });

            app.MapPost("v1/accounts/:handle-debited-event",
                        [Topic("pubsub", "AccountDebited")] async (AccountDebited @event, IActorProxyFactory actorProxyFactory) =>
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
                        [Topic("pubsub", "AccountCredited")] async (AccountCredited @event, IActorProxyFactory actorProxyFactory) =>
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
                        [Topic("pubsub", "InstantAccessSavingsAccountActivated")] async (InstantAccessSavingsAccountActivated @event, IActorProxyFactory actorProxyFactory) =>
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
                        [Topic("pubsub", "AccountInterestAccrued")] async (AccountInterestAccrued @event, IActorProxyFactory actorProxyFactory) =>
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
                        [Topic("pubsub", "Commands")] async (CloudEvent<JsonObject> evt) =>
                        {
                            if (evt != null && evt.Data != null)
                            {
                                var typeProperty = evt.Data["EventType"]!.GetValue<string>();
                                var res = JsonDeserializeHelper.Deserialize(evt.Data, Type.GetType(typeProperty)!);
                            }

                            await Task.Delay(250);
                        });
        }
    }
}
