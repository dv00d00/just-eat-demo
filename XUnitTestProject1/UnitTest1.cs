using System;
using System.Reactive.Linq;
using System.Threading;
using NUnit.Framework;


namespace XUnitTestProject1
{
    public class UnitTest1
    {
        [Test]
        public void Test1()
        {
            var random = new Random();
            var observable = Observable.Interval(TimeSpan.FromSeconds(0.1)).Select(_ => random.Next(2));

            var groupByUntil = observable.GroupByUntil(x => x, _ => Observable.Timer(TimeSpan.FromSeconds(2)));

            groupByUntil.Subscribe(x =>
            {
                Console.WriteLine(x.Key);
                Console.WriteLine(x.Count());
            });

            Thread.Sleep(10000);
        }
    }
}
