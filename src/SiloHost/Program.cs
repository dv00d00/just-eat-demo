using System;
using System.Net;
using System.Threading.Tasks;
using HelloWorld.Grains;
using HelloWorld.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Orleans.Runtime;
using Orleans.Streams;

namespace OrleansSiloHost
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return RunMainAsync().Result;
        }

        private static async Task<int> RunMainAsync()
        {
            try
            {
                var host = await StartSilo();
                Console.WriteLine("Press Enter to terminate...");
                Console.ReadLine();

                await host.StopAsync();

                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task<ISiloHost> StartSilo()
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .AddSimpleMessageStreamProvider(WellKnownIds.StreamProvider)
                .AddMemoryGrainStorage("PubSubStore")
                .UseLocalhostClustering()
                .UseDashboard(options => { options.Port = 8088; })
                .ConfigureServices(sp =>
                {
                    sp.AddTransient(f => f.GetRequiredServiceByName<IStreamProvider>(WellKnownIds.StreamProvider));
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "HelloWorldApp";
                })
                .Configure<EndpointOptions>(options => options.AdvertisedIPAddress = IPAddress.Loopback)
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(Order).Assembly).WithReferences())
                .AddStartupTask(async (sp, ct) =>
                {
                    var grainFactory = sp.GetRequiredService<IGrainFactory>();
                    var database = grainFactory.GetGrain<IDatabase>("elasticsearch-dev");
                    await database.Initialize();
                });
                

            var host = builder.Build();
            await host.StartAsync();
            return host;
        }
    }
}
