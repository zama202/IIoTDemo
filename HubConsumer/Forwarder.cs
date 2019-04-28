using Common.Infrastructure;
using DeviceInterface;
using Microsoft.Azure.Devices;
using System;
using System.Collections.Generic;
using System.Text;

namespace HubConsumer
{
    class Forwarder : IDeviceObserver
    {
        public void ForwardTo(string message, string action)
        {

            Console.WriteLine("Forwarder!!");


            if (action.Equals("HW001"))
            {
                ServiceClient client = IotHubProvider.GetClient();
                IotHubProvider.InvokeMethod("PLC0001", "SetVariableAsync", "{'k':'WRITE1','v':'0'}", client).GetAwaiter().GetResult();


                Console.WriteLine("raised " + action); //Send to MES, Send to Edge 
                Console.WriteLine(message); //Send to MES, Send to Edge 
            }
        }
    }
}
