using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Concurrency;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    public struct Eq : IEqualityComparer<IGrainWithStringKey>
    {
        public bool Equals(IGrainWithStringKey x, IGrainWithStringKey y)
        {
            if (x == null || y == null) return false;
            return x.GetPrimaryKeyString().Equals(y.GetPrimaryKeyString());
        }

        public int GetHashCode(IGrainWithStringKey obj) => obj.GetPrimaryKeyString().GetHashCode();
    }

    [ImplicitStreamSubscription("Orders")]
    public class RecentOrders : Grain, IRecentOrders
    {
        private readonly ILogger<RecentOrders> _logger;
        private StreamSubscriptionHandle<OrderEvent> _handle;
        private Subject<OrderEvent> _subject;
        private IDisposable disposable;

        public RecentOrders(ILogger<RecentOrders> logger)
        {
            _logger = logger;
            _subject = new Subject<OrderEvent>();
        }

        private HashSet<IOrder> Orders { get; } = new HashSet<IOrder>(new Eq());
        
        public override async Task OnActivateAsync()
        {
            var provider = GetStreamProvider(WellKnownIds.StreamProvider);
            var asyncStream = provider.GetStream<OrderEvent>(WellKnownIds.OrderUpdates, WellKnownIds.StreamOrdersNamespace);
            
            disposable = _subject
                .Where(it => it.Event == Event.Created)
                .Buffer(TimeSpan.FromSeconds(0.5))
                .Subscribe(next =>
                {
                    Console.WriteLine($"Adding orders {next.Count}");

                    foreach (var orderEvent in next)
                    {
                        var order = GrainFactory.GetGrain<IOrder>(orderEvent.Id);
                        this.Orders.Add(order);
                    }
                });

            _handle = await asyncStream.SubscribeAsync((@event, token) =>
            {
                _subject.OnNext(@event);
                return Task.CompletedTask;
            });

            await base.OnActivateAsync();
        }

        public override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
            disposable?.Dispose();
            await _handle.UnsubscribeAsync();
        }

        public Task<string[]> GetOrders()
        {
            return Task.FromResult(Orders.Select(it => it.GetPrimaryKeyString()).ToArray());
        }

        public Task<string[]> GetStates()
        {
            return Task.WhenAll(Orders.Select(x => x.GetState()));
        }
    }
}