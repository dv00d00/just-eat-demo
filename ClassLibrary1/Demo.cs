using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using AutoFixture;

namespace Observables
{
    public class Demo
    {
        public enum EventKind
        {
            Created,
            Delivered,
            OnItsWay,
            AtRestaurant,
            AtAddress,
            DriverAssigned
        }

        class Event
        {
            public string Id;
            public EventKind EventKind;
            public DateTime DateTime;

            public override string ToString() => EventKind.ToString();
        }

        public static void Main()
        {
            var random = new Random();

            var guids = Enumerable
                .Range(0, 5)
                .Select(_ => Guid.NewGuid().ToString("N").Substring(0, 8) )
                .ToArray();

            var fixture = new Fixture();
            fixture.Register(() => guids[random.Next(guids.Length)]);
            fixture.Build<Event>();

            var disposable = Observable
                .Interval(TimeSpan.FromMilliseconds(150))
                .Select(x => fixture.Create<Event>())
                .GroupByUntil(x => x.Id, _ => Observable.Timer(TimeSpan.FromSeconds(3)))
                .SelectMany(x => x.ToList().Select(y => new {lst = y, key = x.Key}))
                .Subscribe(it =>
                {
                    var format = string.Join(", ", it.lst);
                    Console.WriteLine($"{DateTime.Now.TimeOfDay} {it.key}: {format}");
                });

            using (disposable)
            {
                Console.ReadLine();        
            }
        }
    }
}
