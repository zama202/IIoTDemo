using IComm_Library.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsoleClient
{
    public class CustomLogger : ILogger
    {
        private static NLog.Logger Logger;

        public CustomLogger()
        {
            Init();
        }

        public void Init()
        {
            if (Logger == null)
                Logger = NLog.LogManager.GetCurrentClassLogger();
        }

        public void LogDebug(string message)
        {
            Logger.Debug(message);
        }

        public void LogException(string message)
        {
            Logger.Info(message);
        }

        public void LogInfo(string message)
        {
            Logger.Info(message);
        }

        public void LogWarning(string message)
        {
            Logger.Info(message);
        }
    }
}
