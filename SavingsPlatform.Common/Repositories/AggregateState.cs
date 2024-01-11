using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SavingsPlatform.Common.Repositories
{
    public class AggregateState<T> : IAggregateState
    {
        public T? Data { get; set; }

        public bool HasUnpublishedEvents { get; set; } = false;
        public string? UnpublishedEventsJson { get; set; } = default;

        public string? ETag { get; set; } = null;
    }
}
