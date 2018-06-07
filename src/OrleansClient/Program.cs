using HelloWorld.Interfaces;
using Orleans;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            try
            {
                using (var client = await StartClientWithRetries())
                {
                    
                    var (time, count) = await DoClientWork(client);

                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine();
                    Console.WriteLine("Duration " + time);
                    Console.WriteLine("Total events " + count);
                    Console.WriteLine("Throughput " + count * 1.0 / time.TotalSeconds);


                    var i = await client.GetGrain<IPartner>(WellKnownIds.OrderUpdates).TotalHandledEvents();
                    Console.WriteLine("Total handled events as reported by actors = " + i);
                    Console.ReadKey();

                    i = await client.GetGrain<IPartner>(WellKnownIds.OrderUpdates).TotalHandledEvents();
                    Console.WriteLine("Total handled events as reported by actors = " + i);
                    Console.ReadKey();

                    i = await client.GetGrain<IPartner>(WellKnownIds.OrderUpdates).TotalHandledEvents();
                    Console.WriteLine("Total handled events as reported by actors = " + i);
                    Console.ReadKey();

                    i = await client.GetGrain<IPartner>(WellKnownIds.OrderUpdates).TotalHandledEvents();
                    Console.WriteLine("Total handled events as reported by actors = " + i);
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
                        .ConfigureLogging(logging => logging.AddConsole())
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
            const int eventsPerOrder = 120;
            const int ordersCount = 8000;
            
            var orderIds = Enumerable.Range(1, ordersCount).Select(_ => Guid.NewGuid().ToString());
            var orders = orderIds.Select(id => client.GetGrain<IOrder>(id));
            
            var stopwatch = Stopwatch.StartNew();
            
            await orders.ForEachAsync(UpdateOrder, 3000);

            async Task UpdateOrder(IOrder order)
            {
                await order.Handle("created");
                
                for (int i = 0; i < eventsPerOrder; i++)
                {
                    await order.Handle("item added");
                }

                await order.Handle("checked out");
            }

            return (stopwatch.Elapsed, (eventsPerOrder + 2) * ordersCount);
        }
    }

    public static class TaskPlus
    {
        public static async Task<IEnumerable<Task>> ForEachAsync<TIn>(
            this IEnumerable<TIn> inputEnumerable,
            Func<TIn, Task> asyncProcessor,
            int maxDegreeOfParallelism)
        {
            var throttler = new SemaphoreSlim(maxDegreeOfParallelism, maxDegreeOfParallelism);

            var tasks = inputEnumerable.Select(async input =>
            {
                await throttler.WaitAsync();

                try
                {
                    await asyncProcessor(input);
                }
                finally
                {
                    throttler.Release();
                }

            }).ToArray();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch 
            {
                // we are not interested in a single exception
                // which will be randomly chosen by Task.WhenAll
                // instead an array of all tasks with exceptions / results
                // is returned to the caller
            }

            return tasks;
        }
    }
}
