using Orleans.Concurrency;

namespace HelloWorld.Interfaces
{

    public enum Event
    {
        Created,
        Updated,
        Completed
    }

    [Immutable]
    public class OrderEvent
    {
        public string Id { get; set; }
        public Event Event { get; set; }

        public OrderEvent(string id, Event @event)
        {
            Id = id;
            Event = @event;
        }
    }
}