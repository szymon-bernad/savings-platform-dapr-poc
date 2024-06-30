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
using SavingsPlatform.Common.Repositories.Marten;
using Marten;
using Microsoft.Extensions.Logging;
using SavingsPlatform.Common.Helpers;

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    public class SettlementAccountRepository : MartenStateEntryRepositoryBase<SettlementAccountState, SettlementAccountDto>
    {
        public SettlementAccountRepository(
            IDocumentSession docSession,
            IStateMapper<AggregateState<SettlementAccountDto>, SettlementAccountState> mapper,
            IEventPublishingService eventPublishingService,
            ILogger<SettlementAccountRepository> logger)
            : base(docSession, mapper, eventPublishingService, logger)
        {    
        }

    }
}
