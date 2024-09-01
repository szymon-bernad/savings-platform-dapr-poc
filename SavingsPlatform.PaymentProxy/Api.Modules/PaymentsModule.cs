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
                    var mapping = await accountExternalRefService.GetAccountEntryByExternalRef(request.AccountRef);
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
                    var mapping = await accountExternalRefService.GetAccountEntryByExternalRef(request.DebtorAccountRef);
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
              [Topic("pubsub", "accountcreated")] async (AccountCreated evt, IAccountExternalRefService svc) =>
              {
                    if (evt is null)
                    {
                        return;
                    }

                    await svc.StoreAccountMapping(
                        new AccountExternalMappingEntry(
                            evt.ExternalRef,
                            evt.AccountId,
                            evt.AccountType));

                    if (evt.AccountType == Contracts.Accounts.Enums.AccountType.SettlementAccount)
                    {
                        await svc.StorePlatformMapping(new PlatformMappingEntry(evt.PlatformId, evt.ExternalRef, new List<string>()));
                    }      
                    else if (evt.AccountType == Contracts.Accounts.Enums.AccountType.SavingsAccount)
                    {                      
                        var platform = await svc.GetPlatformEntry(evt.PlatformId);
                        if (platform is not null)
                        {
                            var accountRefs = new List<string> { evt.ExternalRef };
                            if(platform.AccountRefs.Any())
                            {
                                accountRefs.AddRange(platform.AccountRefs);
                            }

                            platform = platform with { AccountRefs = accountRefs };
                            await svc.StorePlatformMapping(platform);
                        }
                        else
                        {
                            throw new InvalidOperationException($"Platform with Id = {evt.PlatformId} not found.");
                        }
                    }
              });
        }
    }
}
