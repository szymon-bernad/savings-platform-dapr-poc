using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Contracts.Accounts.Enums;
using Microsoft.Extensions.Logging;

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    internal class SettlementAccountFactory : ISettlementAccountFactory
    {
        private readonly IStateEntryRepository<SettlementAccountState> _repository;
        private readonly ILogger<SettlementAccount> _logger;

        public SettlementAccountFactory(
            IStateEntryRepository<SettlementAccountState> repo,
            ILogger<SettlementAccount> logger)
        {
            _repository = repo;
            _logger = logger;
        }

        public async Task<SettlementAccount> GetInstanceAsync(string? id = null)
        {
            if (id is null)
            {
                return new SettlementAccount(_repository, _logger, null);
            }

            var stateEntry = await _repository.GetAccountAsync(id);
            return new SettlementAccount(_repository, _logger, stateEntry);
        }

        public async Task<SettlementAccount> GetInstanceByExternalRefAsync(string externalRef)
        {
            var stateEntry = (await _repository.QueryAccountsByKeyAsync(
                                            new string[] { "data.externalRef", "data.type" }, 
                                            new string[] { externalRef, $"{nameof(AccountType.SettlementAccount)}" }))
                                        .SingleOrDefault();
            if (stateEntry is not null)
            {
                return new SettlementAccount(_repository, _logger, stateEntry);
            }
            else throw new InvalidOperationException($"Cannot get instance with externalRef = {externalRef}");
        }

        public async Task<SettlementAccount> GetInstanceByPlatformId(string platformId)
        {
            var stateEntry = (await _repository.QueryAccountsByKeyAsync(
                                new string[] { "data.platformId", "data.type" },
                                new string[] { platformId, $"{nameof(AccountType.SettlementAccount)}" }))
                            .SingleOrDefault();
            if (stateEntry is not null)
            {
                return new SettlementAccount(_repository, _logger, stateEntry);
            }
            else throw new InvalidOperationException($"Cannot get instance with PlatformId = {platformId}");
        }
    }
}
