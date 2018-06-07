using System.Collections.Generic;
using System.Threading.Tasks;
using HelloWorld.Interfaces;
using Orleans;
using Orleans.Streams;

namespace HelloWorld.Grains
{
    public class Order : Grain, IOrder
    {
        private readonly IStreamProvider provider;
        private List<string> Events { get; } = new List<string>();
        private int HandledEvents = 0;

        private IAsyncStream<OrderEvent> _asyncStream;

        public Order(IStreamProvider streamProvider)
        {
            provider = streamProvider;
        }

        public override async Task OnActivateAsync()
        {
            _asyncStream = provider.GetStream<OrderEvent>(WellKnownIds.OrderUpdates, WellKnownIds.StreamOrdersNamespace);
            // await _asyncStream.OnNextAsync(new OrderEvent(this.GetPrimaryKeyString(), Event.Created));
            await base.OnActivateAsync();
        }

        public async Task Handle(string @event)
        {
            this.Events.Add(@event);
            this.HandledEvents++;
            
            await Task.Delay(350);
            await _asyncStream.OnNextAsync(new OrderEvent(this.GetPrimaryKeyString(), Event.Updated));
        }

        public Task<string> GetState()
        {
            var stringifiedState = string.Join(", ", Events);
            return Task.FromResult(stringifiedState);
        }

        public Task<int> GetHandledEventsCount()
        {
            return Task.FromResult(HandledEvents);
        }
    }
}