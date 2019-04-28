using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceInterface
{
    public interface IDeviceObserver : Orleans.IGrainObserver
    {
        void ForwardTo(string message, string action);
    }
}
