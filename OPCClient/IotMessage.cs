using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleClient
{
    public class IotMessage
    {
        public string DeviceId { get; set; }
        public List<Payload> Data { get; set; }
        public DateTime Datetime { get; set; }
    }

    public class Payload
    {
        public DateTime datetime { get; set; }
        public string name { get; set; }
        public double value { get; set; }
    }
}
