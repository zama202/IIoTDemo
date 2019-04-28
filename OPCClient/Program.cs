using IComm_Library;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OPC_UA_Library;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace TestConsoleClient
{
    public class Program
    {
        public static DeviceClient _deviceClient;

        public static OPCTag T0;
        public static OPCTag T1;
        public static OPCTag T2;
        public static OPCTag R1;
        public static OPCClient MyClient;

        static void Main(string[] args)
        {
            Execute().GetAwaiter().GetResult();

        }

        static async Task Execute()
        {
        

            try
            {
                GetClient(new HandlerConfig().WithElement("SetVariableAsync", SetVariableAsync).Elements);

                CustomLogger logger = new CustomLogger();

                var opcConnectionString = @"opc.tcp://127.0.0.1:49320";
                MyClient = new OPCClient(opcConnectionString, "", "", logger, "Mytest2", SecurityPolicy.Basic128, MessageSecurity.Sign);

                R1 = (OPCTag)MyClient.AddTag("RAMP1", "ns=2;s=TEST_OPC.RAMP.RAMP1", typeof(float));
                T0 = (OPCTag)MyClient.AddTag("WRITE1", "ns=2;s=TEST_OPC.WRITE.WRITE1", typeof(float));
                T1 = (OPCTag)MyClient.AddTag("WRITE2", "ns=2;s=TEST_OPC.WRITE.WRITE2", typeof(float));
                T2 = (OPCTag)MyClient.AddTag("WRITE3", "ns=2;s=TEST_OPC.WRITE.WRITE3", typeof(float));

                var T3 = (OPCTag)MyClient.AddTag("Cast", "ns=2;s=TEST_OPC.WRITE.WRITESLOW1", typeof(double));
                T3.ReadItem();

                List<string> TagNameList = new List<string>();
                TagNameList.Add(T0.Name);
                TagNameList.Add(T1.Name);
                TagNameList.Add(T2.Name);
                TagNameList.Add(R1.Name);

                var baseName1 = "ns=2;s=TEST_OPC.RAMP.RAMP";
                var baseName2 = "ns=2;s=TEST_OPC.RAND.RAND";

                for (int i = 1; i <= 150; i++)
                {
                    var tagName1 = $"{baseName1}{i}";
                    MyClient.AddTag(tagName1, tagName1, typeof(float));
                    TagNameList.Add(tagName1);

                    var tagName2 = $"{baseName2}{i}";
                    MyClient.AddTag(tagName2, tagName2, typeof(int));
                    TagNameList.Add(tagName2);
                }

                while (true)
                {
                    MyClient.ReadTags(TagNameList);
                    IotMessage message = new IotMessage();
                    message.Datetime = DateTime.Now;
                    message.DeviceId = "PLC0001";
                    message.Data = new List<Payload>();
                    foreach (var x in TagNameList)
                    {

                        double y = 0;
                        if (MyClient.GetTag(x)?.Value != null)
                            y = Double.Parse(MyClient.GetTag(x).Value.ToString());

                        message.Data.Add(new Payload() {datetime = DateTime.Now, name = MyClient.GetTag(x).Name , value = y });
                        Console.WriteLine(MyClient.GetTag(x).Name + " | " + MyClient.GetTag(x).Value);
                    }

                    SendEvent(_deviceClient, message).GetAwaiter().GetResult();

                    Thread.Sleep(100);
                    Console.WriteLine("----");
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static async Task<MethodResponse> SetVariableAsync(MethodRequest methodRequest, object userContext)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Received Command SetVariable");
            string data = Encoding.UTF8.GetString(methodRequest.Data);

            MyClient.WriteTag("WRITE1", Convert.ToSingle(666.0));
            MyClient.ReadTag("WRITE1");
            T0.WriteItem(Convert.ToSingle(666.0));
            T0.ReadItem();
            Console.ReadLine();
            Console.WriteLine("Value was: " + MyClient.GetTag("WRITE1")?.Value);
            Console.WriteLine("Value is change to: " + MyClient.GetTag("WRITE1")?.Value);
            Console.ReadLine();
            string result = "{\"result\":\"Executed direct method: " + methodRequest.Name + "\"}";
            Console.WriteLine(result + " || " + data);
            Console.ResetColor();
            return await Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }


        public static async Task SendEvent(DeviceClient client, IotMessage payload)
        {
            try
            {
                var messageString = JsonConvert.SerializeObject(payload);
                var message = new Message(Encoding.ASCII.GetBytes(messageString));

                // Add a custom application property to the message.
                // An IoT hub can filter on these properties without access to the message body.
                //message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                // Send the tlemetry message
                await client.SendEventAsync(message).ConfigureAwait(false);
                Console.ForegroundColor = ConsoleColor.Cyan;

                Console.WriteLine("{0} > Sending message: {1}", DateTime.Now, messageString);
                Console.ResetColor();

                await Task.Delay(500);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            
        }


        private static void GetClient(SortedList<string, MethodCallback> Handlers)
        {
            try
            {
                Console.WriteLine("start");

                var ConnectionString = "";
                // Create a connection using device context for AMQP session
                _deviceClient = DeviceClient.CreateFromConnectionString(ConnectionString, TransportType.Amqp);
                Console.WriteLine(ConnectionString);

                foreach (KeyValuePair<string, MethodCallback> handler in Handlers)
                {
                    _deviceClient.SetMethodHandlerAsync(handler.Key, handler.Value, null).Wait();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            
        }

    }

    public class HandlerConfig
    {
        public SortedList<string, MethodCallback> Elements { get; set; } = new SortedList<string, MethodCallback>();

        public HandlerConfig WithElement(string methodName, MethodCallback methodImplementation)
        {
            Elements.Add(methodName, methodImplementation);
            return this;
        }
    }



}
