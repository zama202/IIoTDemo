using Orleans.Configuration;
using Orleans.Hosting;
using System;
using System.Threading.Tasks;
using DeviceActor;
using Microsoft.Extensions.Logging;
using Orleans;
using System.Threading;

namespace Plant
{
    class Program
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
                //Console.WriteLine("\n\n Press Enter to terminate...\n\n");
                //                Console.ReadLine();
                //                await host.StopAsync();
                while (true)
                    Thread.Sleep(60000);

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
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "clu001";
                    options.ServiceId = "DeviceManagement";
                })
                .UseLocalhostClustering()
                .ConfigureApplicationParts(parts => parts.AddApplicationPart(typeof(Device).Assembly).WithReferences())
                .ConfigureLogging(logging => logging.AddConsole());
            

            var host = builder.Build();


            

            await host.StartAsync();
            return host;
        }
    }
}
