using MediatR;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Commands;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Handlers
{
    public class PublishEventsCommandHandler : IRequestHandler<PublishEventsCommand>
    {
        private readonly IAggregateRootFactory<SettlementAccount, SettlementAccountState> _settlementFactory;
        private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _instantAccessFactory;


        public PublishEventsCommandHandler( 
            IAggregateRootFactory<SettlementAccount, SettlementAccountState> settlementFactory,
            IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory)
        {
            _settlementFactory = settlementFactory;
            _instantAccessFactory = iasaFactory;
        }

        public async Task Handle(PublishEventsCommand request, CancellationToken cancellationToken)
        {
            if (request.AccountType == AccountType.SettlementAccount)
            {
                var acc = await _settlementFactory.GetInstanceAsync(request.AccountId);
                await acc.TryUpdateAsync();
            }

            if (request.AccountType == AccountType.SavingsAccount)
            {
                var acc = await _instantAccessFactory.GetInstanceAsync(request.AccountId);
                await acc.TryUpdateAsync();
            }
        }
    }
}
