using Dapr.Actors;
using Dapr.Actors.Client;
using MediatR;
using SavingsPlatform.Accounts.Actors;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Handlers
{
    public class TransferDepositCommandHandler : IRequestHandler<TransferDepositCommand>
    {
        private readonly IActorProxyFactory _actorProxyFactory;
        private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _iasaFactory;
        private readonly ISettlementAccountFactory _settlementAccountFactory;

        public TransferDepositCommandHandler(
            IActorProxyFactory actorProxyFactory,
            IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory,
            ISettlementAccountFactory settlementAccountFactory
            )
        {
            _actorProxyFactory = actorProxyFactory;
            _iasaFactory = iasaFactory;
            _settlementAccountFactory = settlementAccountFactory;
        }

        public async Task Handle(TransferDepositCommand request, CancellationToken cancellationToken)
        {
            var transferId = request.TransferId ?? Guid.NewGuid().ToString();
            var actorInstance = _actorProxyFactory.CreateActorProxy<IDepositTransferActor>(
                new ActorId(transferId),
                nameof(DepositTransferActor));

            var dtData = new DepositTransferData
            {
                Amount = request.Amount,
                TransactionId = transferId,
                Direction = request.Direction,
                WaitForAccountCreation = request.WaitForAccountCreation,
            };

            if (!request.WaitForAccountCreation)
            {
                var savingsAcc = await _iasaFactory.GetInstanceByExternalRefAsync(request.SavingsAccountRef);
                var settlementAcc = await _settlementAccountFactory.GetInstanceByPlatformId(savingsAcc.State!.PlatformId);

                dtData = dtData with
                {
                    DebtorAccountId = request.Direction == TransferDirection.FromSavingsAccount ? savingsAcc.State!.Key : settlementAcc.State!.Key,
                    BeneficiaryAccountId = request.Direction == TransferDirection.ToSavingsAccount ? savingsAcc.State!.Key : settlementAcc.State!.Key,
                };
            }

            await actorInstance.InitiateTransferAsync(dtData);
        }
    }
}
