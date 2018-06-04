using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    [ImplicitStreamSubscription(WellKnownIds.StreamOrdersNamespace)]
    public class RecentOrders : Grain, IRecentOrders
    {
        private HashSet<string> OrderIds = new HashSet<string>();
        
        public override async Task OnActivateAsync()
        {
            var provider = GetStreamProvider(WellKnownIds.StreamProvider);

            var input = provider.GetStream<OrderEvent>(WellKnownIds.OrderUpdates, WellKnownIds.StreamOrdersNamespace);
            var output = provider.GetStream<Changes>(WellKnownIds.DatabaseUpdates, WellKnownIds.DatabaseUpdatesNamespace);

            await input.SubscribeAsync((@event, token) =>
            {
                OnEvent(@event);
                return Task.CompletedTask;
            });

            RegisterTimer(_ => SaveOrders(output), null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

            await base.OnActivateAsync();
        }

        private async Task SaveOrders(IAsyncStream<Changes> output)
        {
            if (OrderIds.Count == 0)
            {
                Console.WriteLine("RecentOrders: No updates over last 20 seconds");
                return;
            }

            var orders = OrderIds.Select(x => GrainFactory.GetGrain<IOrder>(x));
            var states = await Task.WhenAll(orders.Select(x => x.GetState()));

            Console.WriteLine($"RecentOrders: Notifing database about new batch; Batch size = {states.Length}");

            output.OnNextAsync(new Changes { NewStates = states });

            OrderIds.Clear();
        }

        private void OnEvent(OrderEvent @event)
        {
            OrderIds.Add(@event.Id);
        }
    }
}