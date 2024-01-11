using Dapr.Client;
using Microsoft.Extensions.Options;
using SavingsPlatform.Accounts.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using System.Net.Http.Json;

namespace SavingsPlatform.Common.Repositories
{
    public class StateEntryRepositoryBase<TEntry, TData> 
        : IStateEntryRepository<TEntry> 
        where TEntry : IAggregateStateEntry
    {
        protected readonly DaprClient _daprClient;
        protected readonly HttpClient _httpClient;
        protected readonly IStateMapper<AggregateState<TData>, TEntry> _mapper;
        protected readonly IEventPublishingService _eventPublishingService;

        protected readonly string StateStoreName;
        protected readonly string PubSubName;

        public StateEntryRepositoryBase(DaprClient daprClient,
            HttpClient httpClient,
            IOptions<SavingsAccountsStateStoreConfig> stateStoreCfg,
            IStateMapper<AggregateState<TData>, TEntry> mapper,
            IEventPublishingService eventPublishingService)
        {
            _daprClient = daprClient;
            _httpClient = httpClient;
            _eventPublishingService = eventPublishingService;

            StateStoreName = stateStoreCfg?.Value?.StateStoreName ?? throw new ArgumentNullException(nameof(StateStoreName));
            PubSubName = stateStoreCfg?.Value?.PubSubName ?? throw new ArgumentNullException(nameof(PubSubName));
            _mapper = mapper;
        }

        public async Task<TEntry?> GetAccountAsync(string key)
        {
            var result = await _daprClient.GetStateEntryAsync<AggregateState<TData>>(
                StateStoreName,
                key);
            if (result?.Value is not null)
            {
                return _mapper.Map(result.Value!);
            }

            return default(TEntry);
        }

        public Task AddAccountAsync(TEntry account)
        {
            //!!! call SaveState via Dapr HTTP API (saves value as serialized JSON which is queryable from DaprClient)
            return TryUpsertAccountAsync(account);
        }

        public Task<bool> TryUpdateAccountAsync(TEntry account)
        {
            return TryUpsertAccountAsync(account);
        }

        public async Task<bool> TryUpsertAccountAsync(TEntry account)
        {
            var isSuccess = await PostToStateStoreAsync(account);

            if (isSuccess)
            {
                if (account.UnpublishedEvents?.Any() ?? false)
                {
                    await _eventPublishingService.PublishEvents(account.UnpublishedEvents);
                    await TryUpsertAfterEventsPublishedAsync(account);
                }
                return true;
            }

            return false;
        }

        public async Task<ICollection<TEntry>> QueryAccountsByKeyAsync(string keyName, string keyValue)
        {
            var filter = GetFilterQuery(keyName, keyValue);
            var result = await _daprClient.QueryStateAsync<AggregateState<TData>>(
                StateStoreName,
                filter,
                new Dictionary<string, string>() { ["metadata.contentType"] = "application/json" });

            if (result.Results?.Any() ?? false)
            {
                var mappedData = result.Results.Select(
                    r =>
                    {
                        var m = _mapper.Map(r.Data);
                        m.Etag = r.ETag;
                        return m;
                    }).ToList();

                return mappedData;
            }

            return Enumerable.Empty<TEntry>().ToList();
        }

        protected virtual string GetFilterQuery(string keyName, string keyValue)
        {
            return $"{{\"filter\":{{\"EQ\":{{\"{keyName}\":\"{keyValue}\"}}}}}}";
        }
        protected async Task<bool> PostToStateStoreAsync(TEntry entry)
        {
            var stateDto = _mapper.ReverseMap(entry);
            var res = await _httpClient.PostAsJsonAsync(
                $"/v1.0/state/{StateStoreName}?metadata.contentType=application/json",
                new object[] { new { key = entry.Key, value = stateDto, etag = entry.Etag } });

            return res?.IsSuccessStatusCode ?? false;
        }

        protected async Task<bool> TryUpsertAfterEventsPublishedAsync(TEntry entry)
        {
            var stateDto = _mapper.ReverseMap(entry);
            stateDto.HasUnpublishedEvents = false;
            stateDto.UnpublishedEventsJson = null;
            var res = await _httpClient.PostAsJsonAsync(
                $"/v1.0/state/{StateStoreName}?metadata.contentType=application/json",
                new object[] { new { key = entry.Key, value = stateDto, etag = entry.Etag } });

            return res?.IsSuccessStatusCode ?? false;
        }
    }
}
