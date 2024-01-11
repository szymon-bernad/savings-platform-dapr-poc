using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    public class SettlementAccountStateMapper : IStateMapper<AggregateState<SettlementAccountDto>, SettlementAccountState>
    {
        public SettlementAccountState Map(AggregateState<SettlementAccountDto> state)
        {
            var events = !string.IsNullOrEmpty(state.UnpublishedEventsJson) ?
                JsonSerializer.Deserialize<IEnumerable<JsonNode>>(state!.UnpublishedEventsJson) : Enumerable.Empty<JsonNode>();

            var unpubEvents = events is not null ? Enumerable.Cast<object>(events.Select(e => e.AsObject())).ToList() : null;

            return new SettlementAccountState
            {
                Key = state.Data!.Key,
                ExternalRef = state.Data!.ExternalRef,
                OpenedOn = state.Data.OpenedOn,
                TotalBalance = state.Data!.TotalBalance,
                LastTransactionId = state.Data.LastTransactionId,
                HasUnpublishedEvents = state.HasUnpublishedEvents,
                UnpublishedEvents = unpubEvents
            };
        }

        public AggregateState<SettlementAccountDto> ReverseMap(SettlementAccountState dto)
        {
            return new AggregateState<SettlementAccountDto>
            {
                Data = new SettlementAccountDto(dto.Key, dto.ExternalRef, dto.OpenedOn, dto.TotalBalance, dto.LastTransactionId, dto.Type),
                HasUnpublishedEvents = dto.HasUnpublishedEvents,
                UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
                    JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) : null
            };
        }
    }
}
