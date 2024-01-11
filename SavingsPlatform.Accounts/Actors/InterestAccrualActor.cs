using Dapr.Actors.Runtime;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Models;

namespace SavingsPlatform.Accounts.Actors
{
    public class InterestAccrualActor : Actor, IInterestAccrualActor, IRemindable
    {
        private const string InterestAccrualState = nameof(InterestAccrualState);
        private const string DailyAccrualAttempt = nameof(DailyAccrualAttempt);

        private readonly IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> _iasaFactory;
        private readonly SimulationConfig _simulationConfig;

        public InterestAccrualActor(
            IAggregateRootFactory<InstantAccessSavingsAccount, InstantAccessSavingsAccountState> iasaFactory,
            IOptions<SimulationConfig> simulationConfig,
            ActorHost host) : base(host)
        {
            _iasaFactory = iasaFactory;
            _simulationConfig = simulationConfig?.Value ?? new SimulationConfig { SpeedMultiplier = 1 };
        }

        public async Task InitiateAsync(InterestAccrualData data)
        {
            var timespan = TimeSpan.FromMinutes(24 * 60 / _simulationConfig.SpeedMultiplier);
            await RegisterReminderAsync(
                DailyAccrualAttempt,
                null,
                timespan,
                timespan);

            await RunAccrualAttempt(data);
        }


        public async Task HandleAccruedEventAsync(DateTime timestamp)
        {
            var actorData = await StateManager.GetStateAsync<InterestAccrualData>(InterestAccrualState);
            if (actorData is not null)
            {
                actorData = actorData with { LastAccrualDate = timestamp };
                await StateManager.SetStateAsync(InterestAccrualState, actorData);

            }
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            var actorData = await StateManager.GetStateAsync<InterestAccrualData>(InterestAccrualState);
            var res = reminderName switch
            {
                DailyAccrualAttempt => RunAccrualAttempt(actorData),
                _ => Task.CompletedTask
            };

            await res;
        }

        private async Task RunAccrualAttempt(InterestAccrualData data)
        {
            if (data.LastAccrualDate is null)
            {
                await StateManager.SetStateAsync(InterestAccrualState, data);
            }

            var iasa = await _iasaFactory.GetInstanceAsync(data.AccountKey);
            await iasa.AccrueInterest(data.LastAccrualDate, DateTime.UtcNow);
        }
    }
}
