using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Json;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Enums;

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    public class SettlementAccountRepository : StateEntryRepositoryBase<SettlementAccountState, SettlementAccountDto>
    {
        public SettlementAccountRepository(
            DaprClient daprClient,
            HttpClient httpClient,
            IOptions<SavingsAccountsStateStoreConfig> stateStoreCfg,
            IStateMapper<AggregateState<SettlementAccountDto>, SettlementAccountState> mapper,
            IEventPublishingService eventPublishingService)
            : base(daprClient, httpClient, stateStoreCfg, mapper, eventPublishingService)
        {    
        }

        protected override string GetFilterQuery(string keyName, string keyValue, bool isKeyValueAString = false)
        {
            var keyValuePart = string.IsNullOrWhiteSpace(keyValue) ? 
                                "" :
                                $"{{\"EQ\":{{\"{keyName}\":{(isKeyValueAString ? $"\"{keyValue}\"" : keyValue)}}}}},";

            if (string.IsNullOrWhiteSpace(keyValuePart))
            {
                return base.GetFilterQuery("data.type", $"{(int)AccountType.SettlementAccount}", false);
            }

            return $"{{\"filter\":{{\"AND\":[" +
                keyValuePart +
                $"{{\"EQ\":{{\"data.type\":{(int)AccountType.SettlementAccount}}}}}" +
                $"]}}}}";
        }
    }
}
