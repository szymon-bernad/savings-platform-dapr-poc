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

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    internal class SettlementAccountFactory : IAggregateRootFactory<SettlementAccount, SettlementAccountState>
    {
        private readonly IStateEntryRepository<SettlementAccountState> _repository;

        public SettlementAccountFactory(IStateEntryRepository<SettlementAccountState> repo)
        {
            _repository = repo;
        }

        public async Task<SettlementAccount> GetInstanceAsync(string? id = null)
        {
            if (id is null)
            {
                return new SettlementAccount(_repository, null);
            }

            var stateEntry = await _repository.GetAccountAsync(id);
            return new SettlementAccount(_repository, stateEntry);
        }

        public async Task<SettlementAccount> GetInstanceByExternalRefAsync(string externalRef)
        {
            var stateEntry = (await _repository.QueryAccountsByKeyAsync(
                                            new string[] { "data.externalRef", "data.type" }, 
                                            new string[] { externalRef, $"{nameof(AccountType.SettlementAccount)}" }))
                                        .SingleOrDefault();
            if (stateEntry is not null)
            {
                return new SettlementAccount(_repository, stateEntry);
            }
            else throw new InvalidOperationException($"Cannot get instance with externalRef = {externalRef}");
        }
    }
}
