using System.Collections.Generic;
using Orleans.Concurrency;

namespace HelloWorld.Interfaces
{
    [Immutable]
    public class Changes
    {
        public IReadOnlyCollection<string> NewStates { get; set; }
    }
}