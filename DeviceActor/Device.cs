using DeviceInterface;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using System.Text;
using RestSharp;

namespace DeviceActor
{
    public class Device : Orleans.Grain, IDevice
    {
        private readonly ILogger logger;

        private string Status { get; set; }
        private List<string> alerts { get; set; }

        public Device(ILogger<Device> logger)
        {
            this.logger = logger;
        }

        Task<string> IDevice.SetStatus(string status)
        {
            Status = status;
            logger.LogInformation($"\n SetStatus message received: status = '{status}'");
            return Task.FromResult($"\n Client said: '{status}', so Actor says: OK, modified!");
        }

        Task<string> IDevice.AddAlert(string alertCode)
        {
            if (null == alerts)
                alerts = new List<string>();

            alerts.Add(alertCode);
            logger.LogInformation($"\n alertCode message received: added = '{alertCode}'");

            string currentAlerts = "";
            foreach (string alert in alerts)
                currentAlerts += alert + ",";

            return Task.FromResult($"\n Client said: '{alertCode}', so Actor says: OK, Added! - CurrentAlerts: '{currentAlerts}'");
        }

        public Task<string> Evaluate(string m)
        {
            
            //string result = "done";
            string result = generateCorrelationId();

            JObject message = JObject.Parse(m);

            if (pendingOperations.Count > 3)
                Publish("aiuto!!!! Spegni la macchina, chiama il prete!", "HW001");

            return Task.FromResult(result);

        }
        OrleansObserverSubscriptionManager<IDeviceObserver> subscribers;

        private Dictionary<string, string> pendingOperations = new Dictionary<string, string>();

        private string generateCorrelationId() {
            string guid = Guid.NewGuid().ToString();
            pendingOperations.Add(guid, "Pending");

            Console.WriteLine("pendingOperations: " + pendingOperations.Keys.Count);
            
            return guid;
        }

        public Task Subscribe(IDeviceObserver observer)
        {
            Console.WriteLine("[OnServer] Observer Subscribed ");

            subscribers.Subscribe(observer);
            return Task.CompletedTask;
        }

        public Task Publish(string message, string action)
        {
            Console.WriteLine("[OnServer] Publish Notified to subscribers => " + subscribers.Count);
            
            subscribers.Notify(x => x.ForwardTo(message, action));
            return Task.CompletedTask;
        }

        public override async Task OnActivateAsync()
        {
            Console.WriteLine("OnActivateAsync");
            // We created the utility at activation time.
            subscribers = new OrleansObserverSubscriptionManager<IDeviceObserver>();
            
            await base.OnActivateAsync();
        }

        public Task<int> GetPendingOperationsCount()
        {
            return Task.FromResult(pendingOperations.Count);
        }
    }

}
