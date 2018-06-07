using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Akka;
using Akka.Actor;
using Akka.Streams;
using Akka.Streams.Dsl;
using Akka.Util.Internal;
using AutoFixture;
using Reactive.Streams;

namespace Reactive.Tweets
{
    static class Program
    {
        const int MAX_CONCURENT_ORDERS = 200_000;

        static void Main(string[] args)
        {
            var random = new Random();
            
            var ids = new[] {"A", "B", "C", "D", "E", "F"};

            var fixture = new Fixture();
            fixture.Register(() => ids[random.Next(ids.Length)]);

            var source = Source
                .Tick(TimeSpan.Zero, TimeSpan.FromSeconds(0.2), 0)
                .Select(_ => fixture.Create<Event>());

            var flow = Flow.Create<Event>()
                .GroupBy(MAX_CONCURENT_ORDERS, x => x.Id)
                .GroupedWithin(100, TimeSpan.FromSeconds(5))
                .Select(x => x.OrderBy(it => it.DateTime).ToArray())
                .MergeSubstreams()
                .AsInstanceOf<Flow<Event, Event[], NotUsed>>();

            var sink = Sink.ForEach<Event[]>(x =>
            {
                var id = x.First().Id;
                Console.WriteLine($"{DateTime.Now:HH:mm:ss} {id} [{string.Join<Event>("; ", x)}]");
            });

            using (var sys = ActorSystem.Create("Streams-Sample"))
            using (var mat = sys.Materializer())
            {
                var (handle, task) = source.Via(flow).ToMaterialized(sink, Keep.Both).Run(mat);
                Console.ReadLine();
                handle.Cancel();
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

        public override string ToString() => EventKind.ToString();
    }
}