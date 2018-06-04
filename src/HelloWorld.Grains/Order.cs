using System.Collections.Generic;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    public class Order : Grain, IOrder
    {
        private readonly List<string> _events = new List<string>();
        private IAsyncStream<OrderEvent> _asyncStream;

        public override async Task OnActivateAsync()
        {
            var provider = GetStreamProvider(WellKnownIds.StreamProvider);
            _asyncStream = provider.GetStream<OrderEvent>(WellKnownIds.OrderUpdates, WellKnownIds.StreamOrdersNamespace);

            await _asyncStream.OnNextAsync(new OrderEvent(this.GetPrimaryKeyString(), Event.Created));
            await base.OnActivateAsync();
        }

        public async Task Handle(string @event)
        {
            _events.Add(@event);
            await _asyncStream.OnNextAsync(new OrderEvent(this.GetPrimaryKeyString(), Event.Updated));
        }

        public Task<string> GetState()
        {
            var stringifiedState = string.Join(", ", _events);
                
            return Task.FromResult(stringifiedState);
        }
    }
}