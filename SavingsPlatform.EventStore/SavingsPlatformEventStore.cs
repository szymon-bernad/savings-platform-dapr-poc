﻿using Marten;
using SavingsPlatform.EventStore.Models;
using IEvent = SavingsPlatform.Contracts.Accounts.Interfaces.IEvent;

namespace SavingsPlatform.EventStore
{
    public class SavingsPlatformEventStore
    {
        private readonly IDocumentStore _documentStore;
        private readonly ILogger<SavingsPlatformEventStore> _logger;
        public SavingsPlatformEventStore(
            IDocumentStore documentStore,
            ILogger<SavingsPlatformEventStore> logger)
        {
            this._documentStore = documentStore;
            this._logger = logger;
        }

        public async Task AppendEvents(Guid platformId, IEnumerable<IEvent> events)
        {
            try
            {
                using var session = _documentStore.LightweightSession(System.Data.IsolationLevel.ReadCommitted);
                var streamState = await session.Events.FetchStreamAsync(platformId);
                if (streamState != null)
                {
                    session.Events.Append(platformId, events);
                }
                else
                {
                    session.Events.StartStream<SavingsPlatformOverview>(platformId, events);
                }

                await session.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update Stream: {ex.Message}");
                throw;
            }

        }

        public async Task<SavingsPlatformOverview?> GetPlatformOverview(Guid platformId)
        {
            using var session = _documentStore.LightweightSession(System.Data.IsolationLevel.ReadCommitted);
            return await session.Events.AggregateStreamAsync<SavingsPlatformOverview>(platformId);
        }
    }
}
