using Dapr;
using Dapr.Client;
using Marten;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Common.Helpers;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using SavingsPlatform.Common.Repositories.Marten;
using SavingsPlatform.Common.Services;
using SavingsPlatform.Contracts.Accounts.Enums;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess
{
    internal class InstantAccessSavingsAccountRepository : MartenStateEntryRepositoryBase<InstantAccessSavingsAccountState, InstantAccessSavingsAccountDto>
    {
        public InstantAccessSavingsAccountRepository(
            IDocumentSession docSession,
            IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState> mapper,
            IEventPublishingService eventPublishingService,
            ILogger<MartenStateEntryRepositoryBase<InstantAccessSavingsAccountState, InstantAccessSavingsAccountDto>> logger)
            : base(docSession, mapper, eventPublishingService, logger)
        {
        }
    }
}
