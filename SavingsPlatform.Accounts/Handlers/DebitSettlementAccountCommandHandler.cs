using MediatR;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Accounts.Aggregates.Settlement;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Contracts.Accounts.Requests;

namespace SavingsPlatform.Accounts.Handlers
{
    public class DebitSettlementAccountCommandHandler : IRequestHandler<DebitSettlementAccount>
    {
        private readonly ISettlementAccountFactory _aggregateFactory;
        private readonly IThreadSynchronizer _threadSynchronizer;
        private readonly ILogger _logger;

        public DebitSettlementAccountCommandHandler(
            ISettlementAccountFactory aggregateFactory,
            IThreadSynchronizer threadSynchronizer,
            ILogger<DebitSettlementAccountCommandHandler> logger)
        {
            _aggregateFactory = aggregateFactory;
            _threadSynchronizer = threadSynchronizer;
            _logger = logger;
        }

        public async Task Handle(DebitSettlementAccount request, CancellationToken cancellationToken)
        {
            try
            {
                await _threadSynchronizer.ExecuteSynchronizedAsync(
                    request.AccountId,
                    async () =>
                    {
                        var instance = await _aggregateFactory.GetInstanceAsync(request.AccountId);
                        await instance.DebitAsync(request);
                    });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"Error debiting settlement account: {ex.Message}");

                throw;
            }
        }
    }
}
