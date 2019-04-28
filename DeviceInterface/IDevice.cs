using System.Threading.Tasks;

namespace DeviceInterface
{
    public interface IDevice : Orleans.IGrainWithStringKey
    {
        Task<string> SetStatus(string status);
        Task<string> AddAlert(string alertCode);
        Task<string> Evaluate(string message);
        Task<int> GetPendingOperationsCount();


        Task Subscribe(IDeviceObserver observer);
        Task Publish(string message, string action);
    }
}
