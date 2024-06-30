using MediatR;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.Accounts.Handlers
{
    public class DebitSettlementAccountCommandHandler : IRequestHandler<DebitSettlementAccount>
    {
        private readonly ISettlementAccountFactory _aggregateFactory;
        private readonly IThreadSynchronizer _threadSynchronizer;

        public DebitSettlementAccountCommandHandler(
            ISettlementAccountFactory aggregateFactory,
            IThreadSynchronizer threadSynchronizer)
        {
            _aggregateFactory = aggregateFactory;
            _threadSynchronizer = threadSynchronizer;
        }

        public async Task Handle(DebitSettlementAccount request, CancellationToken cancellationToken)
        {
            await _threadSynchronizer.ExecuteSynchronizedAsync(
                request.AccountId, 
                async () =>
                {
                    var instance = await _aggregateFactory.GetInstanceAsync(request.AccountId);
                    await instance.DebitAsync(request);
                });

        }
    }
}
