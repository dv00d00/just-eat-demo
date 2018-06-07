using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    [ImplicitStreamSubscription(WellKnownIds.StreamOrdersNamespace)]
    public class Partner : Grain, IPartner
    {
        private readonly IStreamProvider provider;

        private HashSet<string> AllSeenOrderIds { get; }= new HashSet<string>();
        private HashSet<string> OrderIds  = new HashSet<string>();
        private int TotalEvents { get; set; } = 0;

        public Partner(IStreamProvider provider)
        {
            this.provider = provider;
        }

        public override async Task OnActivateAsync()
        {
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

        private void OnEvent(OrderEvent @event)
        {
            TotalEvents++;
            OrderIds.Add(@event.Id);
            AllSeenOrderIds.Add(@event.Id);
        }

        public async Task<(int byOrders, int byPartner)> TotalHandledEvents()
        {
            Console.WriteLine("Partner: TotalHandledEvents start");

            var orders = AllSeenOrderIds.Select(x => GrainFactory.GetGrain<IOrder>(x));
            var sums = await Task.WhenAll(orders.Select(o => o.GetHandledEventsCount()));
            return (sums.Sum(), TotalEvents);
        }

        private async Task SaveOrders(IAsyncStream<Changes> output)
        {
            var set = Interlocked.Exchange(ref OrderIds, new HashSet<string>());
            await Task.Yield();
            
            if (set.Count == 0)
            {
                Console.WriteLine("Partner: No updates over last 5 seconds");
            }
            else
            {
                Console.WriteLine($"Partner: Sending {set.Count} events to database");
                var orders = set.Select(x => GrainFactory.GetGrain<IOrder>(x));
                var states = await Task.WhenAll(orders.Select(x => x.GetState()));

                // when using sqs to deliver messages, task is finished once message was accepted by sqs host
                await output.OnNextAsync(new Changes { NewStates = states });
            }
        }
    }
}