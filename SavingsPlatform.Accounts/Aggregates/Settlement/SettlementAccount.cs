using Microsoft.Extensions.Logging;
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
        private readonly ILogger<SettlementAccount> _loggerInstance;
        public SettlementAccount(
            IStateEntryRepository<SettlementAccountState> repository,
            ILogger<SettlementAccount> logger,
            SettlementAccountState? state = default) : base(repository, state, logger) 
        {
            _loggerInstance = logger;
        }

        public async Task CreateAsync(CreateSettlementAccount request)
        {
            if (request is null || string.IsNullOrEmpty(request.ExternalRef))
            {
                _loggerInstance.LogError($"Cannot create SettlementAccount with null {nameof(request)} or {nameof(request.ExternalRef)}");
                throw new InvalidOperationException($"{nameof(request.ExternalRef)} cannot be null");
            }

            if (_state is not null)
            {
                _loggerInstance.LogError($"SettlementAccount with {nameof(SettlementAccountState.ExternalRef)} = {request.ExternalRef} already exists");

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
            _loggerInstance.LogInformation($"SettlementAccount with {nameof(SettlementAccountState.ExternalRef)} = {request.ExternalRef} created successfully");
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
                        _state.Type,
                        _state.PlatformId)
                };

            _state.LastTransactionId = transactionId;
            _state.TotalBalance += request.Amount;
            _state.HasUnpublishedEvents = true;
            _state.UnpublishedEvents = eventsToPublish.ToList();

            await TryUpdateAsync();
        }

        public async Task DebitAsync(DebitSettlementAccount request)
        {
            _logger.LogInformation($"SettlementAccount {_state.ExternalRef}:" +
                $"BEFORE [TotalBalance = {_state.TotalBalance}, Amount = {request.Amount}, Version = {_state.Version}]");
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
                    _state.Type,
                    _state.PlatformId)
            };

            await TryUpdateAsync();

            _logger.LogInformation($"SettlementAccount {_state.ExternalRef}:" +
                $"AFTER [TotalBalance = {_state.TotalBalance}, Amount = {request.Amount}, Version = {_state.Version}]");
        }
    }
}
