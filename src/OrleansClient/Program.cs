using HelloWorld.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Orleans.Configuration;

namespace OrleansClient
{
    /// <summary>
    /// Orleans test silo client
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {

            await Task.Delay(2500);

            try
            {
                using (var client = await StartClientWithRetries())
                {
                    var (time, count) = await DoClientWork(client);

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();

                    Console.WriteLine("Duration: " + time);
                    Console.WriteLine("Total events: " + count);
                    Console.WriteLine("Throughput: " + count * 1.0 / time.TotalSeconds);

                    var partner = client.GetGrain<IPartner>(WellKnownIds.OrderUpdates);

                    Console.WriteLine("Total handled events as reported by actors = " + await partner.TotalHandledEvents());
                    Console.ReadKey();
                }

                return 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Console.ReadKey();
                return 1;
            }
        }

        private static async Task<IClusterClient> StartClientWithRetries(int initializeAttemptsBeforeFailing = 5)
        {
            int attempt = 0;
            IClusterClient client;
            while (true)
            {
                try
                {
                    client = new ClientBuilder()
                        .UseLocalhostClustering()
                        .Configure<ClusterOptions>(options =>
                        {
                            options.ClusterId = "dev";
                            options.ServiceId = "HelloWorldApp";
                        })
                        .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(IOrder).Assembly).WithReferences())
                        //.ConfigureLogging(logging => logging.AddConsole())
                        .Build();

                    await client.Connect();
                    Console.WriteLine("Client successfully connect to silo host");
                    break;
                }
                catch (SiloUnavailableException)
                {
                    attempt++;
                    Console.WriteLine($"Attempt {attempt} of {initializeAttemptsBeforeFailing} failed to initialize the Orleans client.");
                    if (attempt > initializeAttemptsBeforeFailing)
                    {
                        throw;
                    }
                    await Task.Delay(TimeSpan.FromSeconds(4));
                }
            }

            return client;
        }

        private static async Task<(TimeSpan, int)> DoClientWork(IClusterClient client)
        {
            const int eventsPerOrder = 240;
            const int ordersCount = 3000;
            
            var orderIds = Enumerable.Range(1, ordersCount).Select(_ => Guid.NewGuid().ToString());
            var orders = orderIds.Select(id => client.GetGrain<IOrder>(id)).ToArray();
            
            await orders.ForEachAsync(order => order.Handle("created"), 3000);

            var stopwatch = Stopwatch.StartNew();
            
            await orders.ForEachAsync(UpdateOrder, 3000);

            async Task UpdateOrder(IOrder order)
            {
                // await order.Handle("created");
                
                for (int i = 0; i < eventsPerOrder; i++)
                {
                    await order.Handle("item added");
                }

                await order.Handle("checked out");
            }

            return (stopwatch.Elapsed, (eventsPerOrder + 2) * ordersCount);
        }
    }
}
