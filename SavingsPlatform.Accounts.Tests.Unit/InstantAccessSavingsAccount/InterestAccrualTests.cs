using FluentAssertions;
using NSubstitute;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using Xunit;

namespace SavingsPlatform.Accounts.Tests.Unit.InstantAccessSavingsAccount
{
    public class InterestAccrualTests
    {
        private readonly IStateEntryRepository<InstantAccessSavingsAccountState> repository;
        private readonly SimulationConfig simulationConfig;
        private readonly InstantAccessSavingsAccountState state;

        public InterestAccrualTests()
        {
            repository = Substitute.For<IStateEntryRepository<InstantAccessSavingsAccountState>>();
            simulationConfig = new SimulationConfig { SpeedMultiplier = 1 };

            state = new InstantAccessSavingsAccountState
            {
                Key = Guid.NewGuid().ToString("D"),
                ExternalRef = "Test-IASA",
                ActivatedOn = DateTime.UtcNow.AddMonths(-1),
                HasUnpublishedEvents = false,
                InterestApplicationDueOn = DateTime.UtcNow,
                InterestApplicationFrequency = ProcessFrequency.Daily,
                AccruedInterest = 1m,
                InterestRate = 5.15m,
                OpenedOn = DateTime.UtcNow.AddMonths(-1),
                TotalBalance = 10_001m,
                Type = AccountType.SavingsAccount,
            };
        }

        [Fact]
        public async Task AccrueInterest_GivenValidState_InterestApplicationDue_ShouldAccrueAndApply()
        {
            // arrange
            repository.TryUpdateAccountAsync(state).ReturnsForAnyArgs(Task.FromResult(true));
            var acc = new Aggregates.InstantAccess.InstantAccessSavingsAccount(repository, simulationConfig, state);
            var interestDue = state.InterestApplicationDueOn!.Value;
            // act
            await acc.AccrueInterest(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            // assert
            await repository.Received(1).TryUpdateAccountAsync(Arg.Any<InstantAccessSavingsAccountState>());

            acc.State!.TotalBalance.Should().BeGreaterThan(state.TotalBalance + 1m);
            acc.State!.AccruedInterest.Should().Be(0m);
            acc.State!.InterestApplicationDueOn.Should().Be(interestDue.AddDays(1));   
            acc.State!.HasUnpublishedEvents.Should().BeTrue();
            acc.State!.UnpublishedEvents!.OfType<AccountInterestAccrued>().Count().Should().Be(1);
            acc.State!.UnpublishedEvents!.OfType<AccountInterestApplied>().Count().Should().Be(1);
        }

        [Fact]
        public async Task AccrueInterest_GivenValidState_InterestApplicationNotDue_ShouldAccrueInterest()
        {
            // arrange
            var accState = state with
            {
                InterestApplicationDueOn = DateTime.UtcNow.AddDays(1),
            };

            repository.TryUpdateAccountAsync(state).ReturnsForAnyArgs(Task.FromResult(true));
            var acc = new Aggregates.InstantAccess.InstantAccessSavingsAccount(repository, simulationConfig, accState);

            // act
            await acc.AccrueInterest(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);

            // assert
            await repository.Received(1).TryUpdateAccountAsync(Arg.Any<InstantAccessSavingsAccountState>());

            acc.State!.TotalBalance.Should().Be(state.TotalBalance);
            acc.State!.AccruedInterest.Should().BeGreaterThan(accState.AccruedInterest);
            acc.State!.HasUnpublishedEvents.Should().BeTrue();
            acc.State!.UnpublishedEvents!.OfType<AccountInterestAccrued>().Count().Should().Be(1);
            acc.State!.UnpublishedEvents!.OfType<AccountInterestApplied>().Count().Should().Be(0);
        }

        [Fact]
        public async Task AccrueInterest_GivenValidState_InterestApplicationNotDue_SpeedMultiplied_ShouldAccrueInterest()
        {
            // arrange
            var accState = state with
            {
                InterestApplicationDueOn = DateTime.UtcNow.AddDays(1),
            };
            simulationConfig.SpeedMultiplier = 1440;

            repository.TryUpdateAccountAsync(state).ReturnsForAnyArgs(Task.FromResult(true));
            var acc = new Aggregates.InstantAccess.InstantAccessSavingsAccount(repository, simulationConfig, accState);

            // act
            await acc.AccrueInterest(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(365));

            // assert
            await repository.Received(1).TryUpdateAccountAsync(Arg.Any<InstantAccessSavingsAccountState>());

            var yearInterest = accState.InterestRate * 0.01m * accState.TotalBalance;
            acc.State!.TotalBalance.Should().Be(state.TotalBalance);
            acc.State!.AccruedInterest.Should().BeInRange(yearInterest + 0.99m, yearInterest + 1.01m);
            acc.State!.HasUnpublishedEvents.Should().BeTrue();
            acc.State!.UnpublishedEvents!.OfType<AccountInterestAccrued>().Count().Should().Be(1);
            acc.State!.UnpublishedEvents!.OfType<AccountInterestApplied>().Count().Should().Be(0);
        }
    }
}
