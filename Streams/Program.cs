using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using AutoFixture;
using Reactive.Streams;

namespace Reactive.Tweets
{
    static class Program
    {
        static void Main(string[] args)
        {
            var random = new Random();

            var guids = new[] {"A", "B", "C", "D", "E"};

            var fixture = new Fixture();
            fixture.Register(() => guids[random.Next(guids.Length)]);

            var source = Source
                .Tick(TimeSpan.Zero, TimeSpan.FromSeconds(0.1), 0)
                .Select(_ => fixture.Create<Event>());

            var flow = (Flow<Event, IEnumerable<Event>, NotUsed>) Flow.Create<Event>()
                .GroupBy(100, x => x.Id)
                .GroupedWithin(100, TimeSpan.FromSeconds(5))
                .MergeSubstreams();

            var sink = Sink.ForEach<IEnumerable<Event>>(x =>
            {
                Console.WriteLine(string.Join("; ", x));
                Console.WriteLine("");
            });

            using (var sys = ActorSystem.Create("Streams-Sample"))
            using (var mat = sys.Materializer())
            {
                // Start Akka.Net stream
                source.Via(flow).ToMaterialized(sink, Keep.Both).Run(mat);
                Console.ReadLine();
            }
        }
    }

    public enum EventKind
    {
        Created,
        Delivered,
        OnItsWay,
        AtRestaurant,
        AtAddress,
        DriverAssigned
    }

    public class Event
    {
        public string Id;
        public EventKind EventKind;
        public DateTime DateTime;

        public override string ToString() => Id + ": " + EventKind.ToString();
    }
}