using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Accounts;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Requests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    public class SettlementAccount : AccountAggregateRootBase<SettlementAccountState>
    {

        public SettlementAccount(
            IStateEntryRepository<SettlementAccountState> repository,
            SettlementAccountState? state = default) : base(repository, state) 
        {
        }

        public async Task CreateAsync(CreateSettlementAccount request)
        {
            if (request is null || string.IsNullOrEmpty(request.ExternalRef))
            {
                throw new InvalidOperationException($"{nameof(request.ExternalRef)} cannot be null");
            }

            if (_state is not null)
            {
                throw new InvalidOperationException(
                    $"SettlementAccount with {nameof(SettlementAccountState.ExternalRef)} = {request.ExternalRef}" +
                    $" already exists");
            }

            var accountId = Guid.NewGuid().ToString();
            var platformId = GuidGenerator.AsGuid(accountId).ToString("N");
            await ThrowIfAlreadyExists(accountId, request.ExternalRef);

            var eventsToPub = new Collection<object>
            {
                new AccountCreated
                {
                    Id = Guid.NewGuid().ToString(),
                    ExternalRef = request.ExternalRef,
                    SettlementAccountRef = request.ExternalRef,
                    AccountId = accountId,
                    PlatformId = platformId,
                    AccountType = AccountType.SettlementAccount,
                    Timestamp = DateTime.UtcNow,
                    EventType = typeof(AccountCreated).Name
                }
            };

            var state = new SettlementAccountState()
            {
                Key = accountId,
                Etag = null,
                ExternalRef = request.ExternalRef,
                OpenedOn = DateTime.UtcNow,
                TotalBalance = decimal.Zero,
                LastTransactionId = null,
                PlatformId = platformId,
                HasUnpublishedEvents = false,
                UnpublishedEvents = Enumerable.Cast<object>(eventsToPub).ToList(),
            };

            await CreateAsync(state);
        }

        public async Task CreditAsync(CreditAccount request)
        {
            ValidateForCredit(request.Amount);

            var transactionId = Guid.NewGuid();
            var eventsToPublish = new object[]
                {
                    new AccountCredited(
                        Guid.NewGuid().ToString(),
                        _state.ExternalRef,
                        _state.Key,
                        request.Amount,
                        request.TransferId,
                        DateTime.UtcNow,
                        typeof(AccountCredited)!.Name,
                        _state.PlatformId)
                };

            _state.LastTransactionId = transactionId;
            _state.TotalBalance += request.Amount;
            _state.HasUnpublishedEvents = true;
            _state.UnpublishedEvents = eventsToPublish.ToList();

            await TryUpdateAsync();
        }

        public async Task DebitAsync(DebitAccount request)
        {
            ValidateForDebit(request.Amount);

            var transactionId = Guid.NewGuid();
            _state.LastTransactionId = transactionId;
            _state.TotalBalance -= request.Amount;
            _state.HasUnpublishedEvents = true;
            _state.UnpublishedEvents = new object[]
            {
                new AccountDebited(
                    Guid.NewGuid().ToString(),
                    _state.ExternalRef,
                    _state.Key,
                    request.Amount,
                    request.TransferId,
                    DateTime.UtcNow,
                    typeof(AccountDebited)!.Name,
                    _state.PlatformId)
            };

            await TryUpdateAsync();
        }
    }
}
