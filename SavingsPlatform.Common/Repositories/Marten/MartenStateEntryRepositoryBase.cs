using Marten;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Services;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Security.Principal;
using System.Text;

namespace SavingsPlatform.Common.Repositories.Marten
{
    public class MartenStateEntryRepositoryBase<TEntry, TData> : IStateEntryRepository<TEntry>
        where TEntry : IAggregateStateEntry
    {
        private readonly IDocumentSession _documentSession;
        private readonly IStateMapper<AggregateState<TData>, TEntry> _mapper;
        private readonly IEventPublishingService _eventPublishingService;

        public MartenStateEntryRepositoryBase(
            IDocumentSession docSession,
            IStateMapper<AggregateState<TData>, TEntry> stateMapper,
            IEventPublishingService publishingService )
        {
            _documentSession = docSession;
            _mapper = stateMapper;
            _eventPublishingService = publishingService;
        }

        public Task AddAccountAsync(TEntry account)
        {
            return TryUpsertAccountAsync(account);
        }

        public Task<bool> TryUpdateAccountAsync(TEntry account, bool dataUpdate = true)
        {
            return TryUpsertAccountAsync(account);
        }

        public async Task<ICollection<TEntry>> QueryAccountsByKeyAsync(string[] keyNames, string[] keyValue, bool isKeyValueAString = true)
        {
            var queryStringBuilder = new StringBuilder();

            foreach (var keyName in keyNames)
            {
                if (queryStringBuilder.Length > 0)
                {
                    queryStringBuilder.Append(" AND ");
                }

                var properties = keyName.Split('.');
                queryStringBuilder.Append("data");
                if (properties.Length > 2)
                {
                    queryStringBuilder.Append(" ->" + string.Join("->", properties.SkipLast(1).Select(p => $" '{p}' ")));
                    queryStringBuilder.Append($" ->> '{properties.Last()}' = ?");
                }
                else if (properties.Length == 2)
                {
                    queryStringBuilder.Append($" -> '{properties[0]}' ->> '{properties[1]}' = ?");
                }
                else
                {
                    queryStringBuilder.Append($" ->> '{properties[0]}' = ?");
                }
            }

            var queryStr = queryStringBuilder.ToString();
            var result = (await _documentSession
                                .QueryAsync<AggregateState<TData>>(queryStr, keyValue))
                                .ToList();
                                
            if (result?.Any() ?? false)
            {
                var mappedData = result.Select(
                    r =>
                    {
                        var m = _mapper.Map(r);
                        m.Etag = r.ETag;
                        return m;
                    }).ToList();

                return mappedData;
            }
            
            return Enumerable.Empty<TEntry>().ToList();
        }

        public async Task<TEntry?> GetAccountAsync(string key)
        {
            var result = (await this.QueryAccountsByKeyAsync(
                                        new string[] { "id" }, new string[] { key }))
                                    .SingleOrDefault();
            if (result is not null)
            {
                return result;
            }

            return default;
        }

        protected Task PostToStateStoreAsync(AggregateState<TData> entry)
        {
            if (entry.Version > 1)
            {
                _documentSession.Update(entry);
            }
            else
            {
                _documentSession.Store(entry);
            }
            return _documentSession.SaveChangesAsync();
        }

        public async Task<bool> TryUpsertAccountAsync(TEntry entry, bool dataUpdate = true)
        {
            try
            {
                var stateDto = _mapper.ReverseMap(entry);
                if (dataUpdate)
                {
                    ++stateDto.Version;
                    await PostToStateStoreAsync(stateDto);
                }

                if (entry.UnpublishedEvents?.Any() ?? false)
                {
                    await _eventPublishingService.PublishEvents(entry.UnpublishedEvents);
                    await TryUpsertAfterEventsPublishedAsync(stateDto);
                }
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        protected Task TryUpsertAfterEventsPublishedAsync(AggregateState<TData> entry)
        {
            entry.HasUnpublishedEvents = false;
            entry.UnpublishedEventsJson = null;
            ++entry.Version;
            return PostToStateStoreAsync(entry);
        }
    }
}
