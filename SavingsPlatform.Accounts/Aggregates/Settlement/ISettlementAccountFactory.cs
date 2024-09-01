using SavingsPlatform.Accounts.Aggregates.Settlement.Models;
using SavingsPlatform.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Accounts.Aggregates.Settlement
{
    public interface ISettlementAccountFactory : IAggregateRootFactory<SettlementAccount, SettlementAccountState>
    {
        public Task<SettlementAccount> GetInstanceByPlatformId(string platformId);
    }
}
