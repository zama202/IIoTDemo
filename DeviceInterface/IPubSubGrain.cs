using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DeviceInterface
{

    public interface IPubSubGrain : Orleans.IGrain
    {
        Task Subscribe(IDeviceObserver observer);
        Task Publish(string message, string action);
    }
}
