using System;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using AutoFixture;

namespace Observables
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

    public class Event
    {
        public string Id;
        public EventKind EventKind;
        public DateTime DateTime;

        public override string ToString() => EventKind.ToString();
    }

    public class Generate
    {
        public static IObservable<Event> Events()
        {
            var random = new Random();

            var guids = new[] {"A", "B", "C", "D", "E", "F"};

            var fixture = new Fixture();
            fixture.Register(() => guids[random.Next(guids.Length)]);
            fixture.Build<Event>();

            return Observable
                .Interval(TimeSpan.FromMilliseconds(150))
                .Select(x => fixture.Create<Event>());
        }
    }

    public class Demo
    {
        public static void Main()
        {
            var disposable = Generate.Events()
                .GroupByUntil(x => x.Id, _ => Observable.Timer(TimeSpan.FromSeconds(5)))
                .SelectMany(x => x.ToList().Select(y => new {lst = y, key = x.Key}))
                .Subscribe(it =>
                {
                    var format = string.Join(", ", it.lst);
                    Console.WriteLine($"{DateTime.Now:mm:ss.fff} {it.key} : {format}");
                });

            using (disposable)
            {
                Console.ReadLine();        
            }
        }
    }
}
