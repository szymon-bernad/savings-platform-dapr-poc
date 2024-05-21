using Dapr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Interfaces
{
    public interface IStateEntryRepository<T> where T : IAggregateStateEntry
    {
        Task<T?> GetAccountAsync(string key);
        Task<ICollection<T>> QueryAccountsByKeyAsync(string[] keyName, string[] keyValue, bool isKeyValueAString = true);
        Task AddAccountAsync(T account);
        Task<bool> TryUpdateAccountAsync(T account, bool dataUpdate = true);
    }
}
