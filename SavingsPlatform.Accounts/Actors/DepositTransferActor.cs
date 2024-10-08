﻿using Dapr.Actors.Runtime;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;
using SavingsPlatform.Contracts.Accounts.Requests;
using SavingsPlatform.Contracts.Payments.Requests;
using DaprClient = Dapr.Client.DaprClient;

namespace SavingsPlatform.Accounts.Actors
{
    public class DepositTransferActor : Actor, IDepositTransferActor, IRemindable
    {
        private readonly ISettlementAccountFactory _settlementAccountFactory;
        private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _iasaFactory;
        private readonly DaprClient _daprClient;
        private readonly ILogger<DepositTransferActor> _logger;
        private readonly IEventPublishingService _eventPublishingService;

        private const string TransferAttempt = nameof(TransferAttempt);
        private const string DepositTransferState = nameof(DepositTransferState);

        public DepositTransferActor(
            ISettlementAccountFactory settlementAccountFactory,
            IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory,
            DaprClient daprClient,
            ActorHost host,
            IEventPublishingService eventPublishingService,
            ILogger<DepositTransferActor> logger) : base(host)
        {
            _settlementAccountFactory = settlementAccountFactory;
            _iasaFactory = iasaFactory;
            _daprClient = daprClient;
            _logger = logger;
            _eventPublishingService = eventPublishingService;
        }

        public async Task InitiateTransferAsync(DepositTransferData data)
        {
            if(data.Status == DepositTransferStatus.DebtorDebited)
            {
                await UnregisterReminderAsync(TransferAttempt);
                return;
            }

            if (data.WaitForAccountCreation)
            {
                data = data with { Status = DepositTransferStatus.AwaitingAccountCreation };
                await StateManager.SetStateAsync(DepositTransferState, data);
            }
            else
            {
                await StartTransfer(data);
            }
        }

        public async Task HandleStartAfterAccountCreation(string savingsAccountId, string settlementAccountId)
        {
            var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
            transferData = transferData with
            {
                DebtorAccountId = transferData.Direction == TransferDirection.FromSavingsAccount ? savingsAccountId : settlementAccountId,
                BeneficiaryAccountId = transferData.Direction == TransferDirection.ToSavingsAccount ? savingsAccountId : settlementAccountId,
                WaitForAccountCreation = false,
            };

            await StartTransfer(transferData);
        }

        private Task StartTransfer(DepositTransferData transferData)
        {
            return transferData.Direction switch
            {
                TransferDirection.ToSavingsAccount => StartTransferFromSettlementToSavings(transferData),
                TransferDirection.FromSavingsAccount => StartTransferFromSavingsToSettlement(transferData),
                _ => throw new InvalidOperationException("Unsupported Transfer.")
            };
        }


        private async Task StartTransferFromSettlementToSavings(DepositTransferData transferData)
        {
            var registerReminder = false;                   
            var settlementAccount = await _settlementAccountFactory.GetInstanceAsync(transferData.DebtorAccountId);

            if (settlementAccount.State!.TotalBalance < transferData.Amount)
            {
                registerReminder = true;
            }
            else
            {
                try
                {
                    await _eventPublishingService.PublishCommand(
                        new DebitSettlementAccount(
                            transferData.DebtorAccountId,
                            transferData.Amount,
                            DateTime.UtcNow,
                            transferData.TransactionId));

                    transferData = transferData with { Status = DepositTransferStatus.DebtorDebited };
                    await StateManager.SetStateAsync(DepositTransferState, transferData);
                    await UnregisterReminderAsync(TransferAttempt);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"Debiting SettlementAccount failed: {ex.Message}");
                    registerReminder = true;
                }
            }

            if(transferData.IsFirstAttempt && registerReminder)
            {
                transferData = transferData with { IsFirstAttempt = false };

                await StateManager.SetStateAsync(DepositTransferState, transferData);

                await RegisterReminderAsync(
                    TransferAttempt,
                    null,
                    TimeSpan.FromMinutes(2),
                    TimeSpan.FromMinutes(2));
            }
        }

        private async Task StartTransferFromSavingsToSettlement(DepositTransferData transferData)
        {
            var savingsAccount = await _iasaFactory.GetInstanceAsync(transferData.DebtorAccountId);

            if (savingsAccount.State!.TotalBalance < transferData.Amount)
            {
                throw new InvalidOperationException($"Cannot withdraw more than account balance");
            }
            else
            {
                await savingsAccount.DebitAsync(new DebitAccount(savingsAccount.State!.ExternalRef!, transferData.Amount, DateTime.UtcNow, transferData.TransactionId));
                transferData = transferData with { Status = DepositTransferStatus.DebtorDebited };
                await StateManager.SetStateAsync(DepositTransferState, transferData);
            }
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            var res = reminderName switch
            {
                TransferAttempt => ReInitiateTransferAsync(),
                _ => Task.CompletedTask
            };

            await res;
        }

        public async Task HandleDebitedEventAsync()
        {
            var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
            if (transferData != null && transferData.Status == DepositTransferStatus.DebtorDebited)
            {
                if (transferData.Direction == TransferDirection.ToSavingsAccount)
                {
                    var savingsAccount = await _iasaFactory.GetInstanceAsync(transferData.BeneficiaryAccountId);
                    await savingsAccount.CreditAsync(new CreditAccount(savingsAccount.State!.ExternalRef, transferData.Amount, DateTime.UtcNow, transferData.TransactionId));
                }
                else if (transferData.Direction == TransferDirection.FromSavingsAccount)
                {
                    var settlementAccount = await _settlementAccountFactory.GetInstanceAsync(transferData.BeneficiaryAccountId);
                    await settlementAccount.CreditAsync(new CreditAccount(settlementAccount.State!.ExternalRef, transferData.Amount, DateTime.UtcNow, transferData.TransactionId));

                }
                else
                {
                    return;
                }

                transferData = transferData with { Status = DepositTransferStatus.BeneficiaryCredited };
                await StateManager.SetStateAsync(DepositTransferState, transferData);
            }
            else if (transferData != null && transferData.Status == DepositTransferStatus.BeneficiaryDebited)
            {
                transferData = transferData with { Status = DepositTransferStatus.Completed };
                await StateManager.SetStateAsync(DepositTransferState, transferData);
            }
        }

        public async Task HandleCreditedEventAsync()
        {
            var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);
            if (transferData != null && transferData.Status == DepositTransferStatus.BeneficiaryCredited)
            {
                if (transferData.Direction == TransferDirection.ToSavingsAccount)
                {
                    transferData = transferData with { Status = DepositTransferStatus.Completed };
                    await StateManager.SetStateAsync(DepositTransferState, transferData);
                }
                else if (transferData.Direction == TransferDirection.FromSavingsAccount)
                {
                    var settlementAccount = await _settlementAccountFactory.GetInstanceAsync(transferData.BeneficiaryAccountId);
                    await _daprClient.InvokeMethodAsync<ProcessOutboundPayment>(
                        "sp-poc-paymentproxy",
                        "v1/outbound-payment",
                        new ProcessOutboundPayment(":unknown:", settlementAccount.State!.ExternalRef, transferData.Amount, DateTime.UtcNow, transferData.TransactionId));

                    transferData = transferData with { Status = DepositTransferStatus.BeneficiaryDebited };
                    await StateManager.SetStateAsync(DepositTransferState, transferData);
                }
            }
        }

        private async Task ReInitiateTransferAsync()
        {
            var transferData = await StateManager.GetStateAsync<DepositTransferData>(DepositTransferState);

            await InitiateTransferAsync(transferData);
        }
            
    }
}
