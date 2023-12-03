using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace Fishman
{
    class LogManager
    {
        public static Logger SetupLogger()
        {
            LoggingConfiguration config = new LoggingConfiguration();

            // ### FILE ###
            FileTarget logfile = new FileTarget()
            {
                FileName = Path.Combine("logs", string.Format("{0}.txt", DateTime.Now.ToString("yyyyMMdd_HHmmss"))),
                Name = "logfile",
                KeepFileOpen = true,
                ConcurrentWrites = false
            };

            // ### CONSOLE ###
            ColoredConsoleTarget logconsole = new ColoredConsoleTarget() { Name = "logconsole" };


            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, logconsole));
            config.LoggingRules.Add(new LoggingRule("*", LogLevel.Trace, logfile));

            NLog.LogManager.Configuration = config;
            return NLog.LogManager.GetCurrentClassLogger();
        }
    }
}
