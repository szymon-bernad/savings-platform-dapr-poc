using Carter;
using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Options;
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
                async(DaprClient daprClient,
                       IAccountExternalRefService accountExternalRefService,
                       IOptions<ProxyConfig> proxyCfg,
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

                        var appName = proxyCfg?.Value?.SavingsPlatformAppName ??
                            throw new ArgumentNullException(nameof(proxyCfg.Value.SavingsPlatformAppName));

                        await daprClient.InvokeMethodAsync<CreditAccount>(
                            appName,
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
                       IOptions<ProxyConfig> proxyCfg,
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

                        var appName = proxyCfg?.Value?.SavingsPlatformAppName ??
                            throw new ArgumentNullException(nameof(proxyCfg.Value.SavingsPlatformAppName));
                        await daprClient.InvokeMethodAsync<DebitAccount>(
                            appName,
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
              [Topic("savingspubsub", "accountcreated")] async (AccountCreated evt, IAccountExternalRefService svc) =>
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
