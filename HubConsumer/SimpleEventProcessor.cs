using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Orleans;
using Orleans.Configuration;
using DeviceInterface;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace HubConsumer
{
    public class SimpleEventProcessor : IEventProcessor
    {
        public Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            Console.WriteLine($"Processor Shutting Down. Partition '{context.PartitionId}', Reason: '{reason}'.");
            return Task.CompletedTask;
        }

        public Task OpenAsync(PartitionContext context)
        {
            Console.WriteLine($"SimpleEventProcessor initialized. Partition: '{context.PartitionId}'");
            return Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            Console.WriteLine($"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> messages)
        {
            IClusterClient client = ConnectClient().GetAwaiter().GetResult();


            foreach (var eventData in messages)
            {
                var data = Encoding.UTF8.GetString(eventData.Body.Array, eventData.Body.Offset, eventData.Body.Count);

                
                string[] words = data.Split(System.Environment.NewLine);
                for (int i = 0; i < words.Length; i++)
                {
                    Console.WriteLine(words[i]);
                    JObject json = JObject.Parse(words[i]);
                    if (json.ContainsKey("src"))
                    {
                        DoClientWork(client, json).GetAwaiter().GetResult();
                        DoClientSpyWork(client, json).GetAwaiter().GetResult();
                    }

                    

                }


                Console.WriteLine($"Message received. Partition: '{context.PartitionId}', Data: '{data}'");

                

            }

            client.Close();

            return context.CheckpointAsync();
        }


        private static async Task<IClusterClient> ConnectClient()
        {

            //IPEndPoint[] ips = new IPEndPoint[] {
            //    new IPEndPoint(IPAddress.Parse("104.46.55.39"), 30000),
            //};

            IClusterClient client;
            client = new ClientBuilder() //.UseStaticClustering(ips)
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "clu001";
                    options.ServiceId = "DeviceManagement";
                })

                .ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect();
            Console.WriteLine("Client successfully connected to silo host \n");
            return client;
        }
        private static async Task DoClientWork(IClusterClient client, JObject Message)
        {
            // example of calling grains from the initialized client
            var deviceSerialNumber = Message.GetValue("src").ToString();

            Console.WriteLine(deviceSerialNumber);
            var friend = client.GetGrain<IDevice>(deviceSerialNumber);
            Console.WriteLine("friend.GetPrimaryKeyLong: " + friend.GetPrimaryKeyString());

            var response = await friend.SetStatus("Good");
            Console.WriteLine("\n\n{0}\n\n", response);
            var response2 = await friend.AddAlert("Alert");
            Console.WriteLine("\n\n{0}\n\n", response);

        }

        private static async Task DoClientSpyWork(IClusterClient client, JObject Message)
        {
            var deviceSerialNumber = Message.GetValue("src").ToString();

            var friend = client.GetGrain<IDevice>(deviceSerialNumber);

            int x = await friend.GetPendingOperationsCount();

            Console.WriteLine("friend.GetPrimaryKeyString: " + friend.GetPrimaryKeyString());
            Console.WriteLine("Count: " + x);

            Console.WriteLine("ALARM TRACER - Evaluate ALARM and increment PendingOperations");


            var response = await friend.Evaluate(Message.ToString());

            Forwarder f = new Forwarder();

            //Create a reference for chat usable for subscribing to the observable grain.
            var obj = await client.CreateObjectReference<IDeviceObserver>(f);
            //Subscribe the instance to receive messages.


            await friend.Subscribe(obj);
            Console.ReadLine();


        }


    }

    

}