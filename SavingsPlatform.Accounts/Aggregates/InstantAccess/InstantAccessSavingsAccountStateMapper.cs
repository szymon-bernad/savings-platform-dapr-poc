﻿using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Common.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Threading.Tasks;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;

namespace SavingsPlatform.Accounts.Aggregates.InstantAccess
{
    public class InstantAccessSavingsAccountStateMapper : IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>
    {
        InstantAccessSavingsAccountState IStateMapper<AggregateState<InstantAccessSavingsAccountDto>, InstantAccessSavingsAccountState>.Map(AggregateState<InstantAccessSavingsAccountDto> state)
        {
            var events = !string.IsNullOrEmpty(state.UnpublishedEventsJson) ?
                JsonSerializer.Deserialize<IEnumerable<JsonNode>>(state!.UnpublishedEventsJson) : Enumerable.Empty<JsonNode>();

            var unpubEvents = events is not null ? Enumerable.Cast<object>(events.Select(e => e.AsObject())).ToList() : null;

            return new InstantAccessSavingsAccountState
            {
                Key = state.Data!.Key,
                ExternalRef = state.Data!.ExternalRef,
                SettlementAccountRef = state.Data!.SettlementAccountRef,
                InterestRate = state.Data!.InterestRate,
                AccruedInterest = state.Data!.AccruedInterest,
                OpenedOn = state.Data.OpenedOn,
                ActivatedOn = state.Data.ActivatedOn,
                TotalBalance = state.Data!.TotalBalance,
                LastTransactionId = state.Data.LastTransactionId,
                HasUnpublishedEvents = state.HasUnpublishedEvents,
                InterestApplicationDueOn = state.Data.InterestApplicationDueOn,
                UnpublishedEvents = unpubEvents
            };
        }

        public AggregateState<InstantAccessSavingsAccountDto> ReverseMap(InstantAccessSavingsAccountState dto)
        {
            return new AggregateState<InstantAccessSavingsAccountDto>
            {
                Data = new InstantAccessSavingsAccountDto(
                    dto.Key,
                    dto.ExternalRef,
                    dto.SettlementAccountRef,
                    dto.OpenedOn,
                    dto.ActivatedOn,
                    dto.InterestRate,
                    dto.TotalBalance,
                    dto.AccruedInterest,
                    dto.LastTransactionId,
                    dto.InterestApplicationFrequency,
                    dto.InterestApplicationDueOn),
                HasUnpublishedEvents = dto.HasUnpublishedEvents,
                UnpublishedEventsJson = dto.UnpublishedEvents?.Any() ?? false ?
                    JsonSerializer.Serialize(Enumerable.Cast<object>(dto.UnpublishedEvents)) : null
            };
        }
    }
}
