using Carter;
using Dapr;
using Dapr.Client;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.Payments.Requests;
using SavingsPlatform.PaymentProxy.Services;

namespace SavingsPlatform.PaymentProxy.Api.Modules
{
    public class PaymentsModule : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            app.MapPost("/v1/inbound-payment",
                async (DaprClient daprClient,
                       IAccountExternalRefService accountExternalRefService,
                       ProcessInboundPayment request) =>
                {
                    var mapping = await accountExternalRefService.GetEntryByExternalRef(request.AccountRef);
                    if (mapping is not null && mapping.AccountId is not null)
                    {
                        if (mapping.Type != Contracts.Accounts.Enums.AccountType.SettlementAccount)
                        {
                            return Results.BadRequest($"Invalid account type: {mapping.Type}. Payment failed.");
                        }

                        var req = new CreditAccount(
                            AccountId: mapping.AccountId,
                            Amount: request.Amount,
                            TransactionDate: request.TransactionDate,
                            null);

                        await daprClient.InvokeMethodAsync<CreditAccount>(
                            "dapr-savings-acc",
                            "v1/settlement-account/:credit",
                            req);

                        return Results.Accepted();
                    }
                    else
                    {
                        return Results.BadRequest($"Account not found");
                    }
                });

            app.MapPost("/v1/outbound-payment",
                async (DaprClient daprClient,
                       IAccountExternalRefService accountExternalRefService, 
                       ProcessOutboundPayment request) =>
                {
                    var mapping = await accountExternalRefService.GetEntryByExternalRef(request.DebtorAccountRef);
                    if (mapping is not null && mapping.AccountId is not null)
                    {
                        if (mapping.Type != Contracts.Accounts.Enums.AccountType.SettlementAccount)
                        {
                            return Results.BadRequest($"Invalid account type: {mapping.Type}. Payment failed.");
                        }

                        await Task.Delay(100);

                        var req = new DebitAccount(
                            AccountId: mapping.AccountId,
                            Amount: request.Amount,
                            TransactionDate: request.TransactionDate,
                            null);

                        await daprClient.InvokeMethodAsync<DebitAccount>(
                            "dapr-savings-acc",
                            "v1/settlement-account/:debit",
                            req);

                        return Results.Accepted();
                    }
                    else
                    {
                        return Results.BadRequest($"Account not found");
                    }
                });

            app.MapPost("/v1/accounts/:handle-created-event",
              [Topic("pubsub", "AccountCreated")] async (AccountCreated evt, IAccountExternalRefService svc) =>
              {
                  if (evt != null)
                  {
                      await svc.StoreAccountMapping(
                      new AccountExternalMappingEntry(evt.ExternalRef!, evt.AccountId!, evt.AccountType));
                  }
              });
        }
    }
}
