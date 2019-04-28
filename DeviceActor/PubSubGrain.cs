using DeviceInterface;
using System;
using System.Collections.Generic;
using System.Text;
using Orleans;
using Orleans.Runtime;
using System.Threading.Tasks;

namespace DeviceActor
{
    

    public class PubSubGrain : Orleans.Grain, IPubSubGrain
    {

        OrleansObserverSubscriptionManager<IDeviceObserver> subscribers = new OrleansObserverSubscriptionManager<IDeviceObserver>();

        public Task Subscribe(IDeviceObserver observer)
        {
            subscribers.Subscribe(observer);
            return Task.CompletedTask;
        }

        public Task Publish(string message, string action)
        {
            subscribers.Notify(x => x.ForwardTo(message, action));
            return Task.CompletedTask;
        }
    }

}
