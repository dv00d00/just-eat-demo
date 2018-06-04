using System;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    [ImplicitStreamSubscription(WellKnownIds.DatabaseUpdatesNamespace)]
    public class Database : Grain, IDatabase
    {
        public override async Task OnActivateAsync()
        {
            var provider = GetStreamProvider(WellKnownIds.StreamProvider);
            var input = provider.GetStream<Changes>(WellKnownIds.DatabaseUpdates, WellKnownIds.DatabaseUpdatesNamespace);
            await input.SubscribeAsync(async (data, token) =>
            {
                Console.WriteLine($"DATABASE: Starting saving of {data.NewStates.Count} updated orders");
                await Task.Delay(TimeSpan.FromSeconds(15));
                Console.WriteLine($"DATABASE: Saved {data.NewStates.Count} updated orders");
            });

            await base.OnActivateAsync();
        }

        public async Task Initialize()
        {
            Console.WriteLine("DATABASE: Initializing database connectivity");
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
}