using Dapr;
using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess
{
    internal class InstantAccessSavingsAccountRepository : StateEntryRepositoryBase<InstantAccessSavingsAccountState, InstantAccessSavingsAccountDto>
    {
        public InstantAccessSavingsAccountRepository(
            DaprClient daprClient,
            HttpClient httpClient,
            IOptions<SavingsAccountsStateStoreConfig> stateStoreCfg,
            IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState> mapper,
            IEventPublishingService eventPublishingService) 
            : base(daprClient, httpClient, stateStoreCfg, mapper, eventPublishingService)
        {
        }

        protected override string GetFilterQuery(string keyName, string keyValue)
        {
            return $"{{\"filter\":{{\"AND\":[" +
                $"{{\"EQ\":{{\"{keyName}\":\"{keyValue}\"}}}}," +
                $"{{\"EQ\":{{\"data.type\":{(int)AccountType.SavingsAccount}}}}}" +
                $"]}}}}";
        }
    }
}
