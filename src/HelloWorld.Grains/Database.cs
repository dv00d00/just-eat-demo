using System;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    public class Database : Grain, IDatabase
    {
        private readonly IStreamProvider provider;

        public Database(IStreamProvider provider)
        {
            this.provider = provider;
        }



        public override async Task OnActivateAsync()
        {
            var input = provider.GetStream<Changes>(WellKnownIds.DatabaseUpdates, WellKnownIds.DatabaseUpdatesNamespace);
            await input.SubscribeAsync(async (data, token) =>
            {
                Console.WriteLine($"DATABASE: Starting saving of {data.NewStates.Count} updated orders");
                await Task.Delay(TimeSpan.FromSeconds(5));
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