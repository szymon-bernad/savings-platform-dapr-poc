using FluentAssertions;
using NSubstitute;
using SavingsPlatform.Accounts.Aggregates.InstantAccess;
using SavingsPlatform.Accounts.Aggregates.InstantAccess.Models;
using SavingsPlatform.Common.Config;
using SavingsPlatform.Common.Interfaces;
using SavingsPlatform.Contracts.Accounts.Enums;
using SavingsPlatform.Contracts.Accounts.Events;
using SavingsPlatform.Contracts.Accounts.Requests;
using Xunit;

namespace SavingsPlatform.Accounts.Tests.Unit.InstantAccessSavingsAccount
{
    public class CreditTests
    {
        private readonly IStateEntryRepository<InstantAccessSavingsAccountState> repository;
        private readonly SimulationConfig simulationConfig;
        private readonly InstantAccessSavingsAccountState state;

        public CreditTests()
        {
            repository = Substitute.For<IStateEntryRepository<InstantAccessSavingsAccountState>>();
            simulationConfig = new SimulationConfig { SpeedMultiplier = 1 };

            state = new InstantAccessSavingsAccountState
            {
                Key = Guid.NewGuid().ToString("D"),
                ExternalRef = "Test-IASA",
                ActivatedOn = DateTime.UtcNow.AddDays(-1),
                HasUnpublishedEvents = false,
                InterestApplicationDueOn = DateTime.UtcNow,
                InterestApplicationFrequency = ProcessFrequency.Daily,
                AccruedInterest = 0m,
                InterestRate = 1.15m,
                SettlementAccountRef = "Test-Settlement",
                OpenedOn = DateTime.UtcNow.AddDays(-1),
                TotalBalance = 101m,
                Type = AccountType.SavingsAccount,
            };
        }

        [Fact]
        public async Task CreditAsync_GivenValidState_NonZeroBalance_ShouldPublishAccountCredited()
        {
            // arrange
            repository.TryUpdateAccountAsync(state).ReturnsForAnyArgs(Task.FromResult(true));
            var acc = new Aggregates.InstantAccess.InstantAccessSavingsAccount(repository, simulationConfig, state);

            // act
            await acc.CreditAsync(
                new CreditAccount(
                    Amount: 10m,
                    TransactionDate: DateTime.UtcNow,
                    TransferId: Guid.NewGuid().ToString("D"),
                    AccountId: state.Key));

            // assert
            await repository.Received(1).TryUpdateAccountAsync(Arg.Any<InstantAccessSavingsAccountState>());

            acc.State!.TotalBalance.Should().Be(state.TotalBalance + 10m);
            acc.State!.HasUnpublishedEvents.Should().BeTrue();
            acc.State!.UnpublishedEvents!.First().Should().BeOfType<AccountCredited>();
        }

        [Fact]
        public async Task CreditAsync_GivenValidState_AccountNotActivated_ShouldPublishEvents()
        {
            // arrange
            repository.TryUpdateAccountAsync(state).ReturnsForAnyArgs(Task.FromResult(true));
            var accState = state with
            {
                ActivatedOn = null,
                InterestApplicationDueOn = null,
                TotalBalance = 0m,
            };

            var acc = new Aggregates.InstantAccess.InstantAccessSavingsAccount(repository, simulationConfig, accState);

            // act
            await acc.CreditAsync(
                new CreditAccount(
                    Amount: 10m,
                    TransactionDate: DateTime.UtcNow,
                    TransferId: Guid.NewGuid().ToString("D"),
                    AccountId: state.Key));

            // assert
            await repository.Received(1).TryUpdateAccountAsync(Arg.Any<InstantAccessSavingsAccountState>());

            acc.State!.TotalBalance.Should().Be(10m);
            acc.State!.ActivatedOn.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5.0));
            acc.State!.HasUnpublishedEvents.Should().BeTrue();
            acc.State!.UnpublishedEvents!.OfType<AccountCredited>().Count().Should().Be(1);
            acc.State!.UnpublishedEvents!.OfType<InstantAccessSavingsAccountActivated>().Count().Should().Be(1);
        }


        [Fact]
        public async Task CreditAsync_GivenValidState_FailedToUpdateRepository_ShouldThrowException()
        {
            // arrange
            repository.TryUpdateAccountAsync(state).ReturnsForAnyArgs(Task.FromResult(false));
            var acc = new Aggregates.InstantAccess.InstantAccessSavingsAccount(repository, simulationConfig, state);

            // act
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await acc.CreditAsync(
                new CreditAccount(
                    Amount: 10m,
                    TransactionDate: DateTime.UtcNow,
                    TransferId: Guid.NewGuid().ToString("D"),
                    AccountId: state.Key)));

            // assert
            await repository.Received(1).TryUpdateAccountAsync(Arg.Any<InstantAccessSavingsAccountState>());
        }
    }
}
