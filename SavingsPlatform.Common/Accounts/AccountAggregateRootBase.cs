using SavingsPlatform.Common.Interfaces;

namespace SavingsPlatform.Common.Accounts
{
    public class AccountAggregateRootBase<T> : IAggregateRoot<T> where T : IAggregateStateEntry
    {
        protected T? _state;
        protected readonly IStateEntryRepository<T> _repository;

        public AccountAggregateRootBase(
            IStateEntryRepository<T> repository,
            T? state = default
            )
        {
            _repository = repository;
            _state = state;
        }

        protected async Task CreateAsync(T state)
        {
            await _repository.AddAccountAsync(state);

            _state = await _repository.GetAccountAsync(state.Key);

            if (_state is null)
            {
                throw new ApplicationException(
                    $"Account has not been persisted in the state store.");
            }
        }

        protected async Task ThrowIfAlreadyExists(string key, string externalRef)
        {
            var res = await _repository.GetAccountAsync(key);

            if (res is not null)
            {
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.Key)} = {key} already exists");
            }

            var queryRes = await _repository.QueryAccountsByKeyAsync(new string[] { "data.externalRef" }, new string[] { externalRef });
            if (queryRes.Any())
            {
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.ExternalRef)} = {externalRef} already exists");
            }
        }

        protected void ValidateForCredit(decimal amount)
        {
            if (_state is null)
            {
                throw new ApplicationException($"Account with invalid state.");
            }

            if (amount <= 0m)
            {
                throw new InvalidOperationException($"Credit transaction amount must be greater than 0.00");
            }
        }

        protected void ValidateForDebit(decimal amount)
        {
            if (_state is null)
            {
                throw new ApplicationException($"Account with invalid state.");
            }
            if (amount <= 0m)
            {
                throw new InvalidOperationException($"Debiy transaction amount must be greater than 0.00");
            }

            if (_state.TotalBalance < amount)
            {
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.Key)} = {_state.Key}" +
                    $" has insufficient funds.");
            }
        }

        public async Task TryUpdateAsync(bool dataUpdate = true)
        {
            var res = await _repository.TryUpdateAccountAsync(_state!);

            if (!res)
            {
                throw new InvalidOperationException(
                    $"Account with {nameof(_state.Key)} = {_state!.Key}" +
                    $" was probably updated by another process");
            }
        }
        public T? State => _state;
    }
}
