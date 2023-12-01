using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace Fishman
{
    class Program
    {
        private static readonly string BotName = "Warcraft Fishman";
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static void Main(string[] args)
        {
            Console.CancelKeyPress += delegate { logger.Warn(Statistics.GetReport()); };
            LogManager.SetupLogger();
            logger.Info("{0} ver. {1}", BotName, Assembly.GetEntryAssembly().GetName().Version.ToString());
            logger.Info("Author: Riketta. Feedback: https://github.com/riketta");

            Arguments arguments = null;
            var result = Parser.Default.ParseArguments<Arguments>(args).WithParsed(opts => arguments = opts);

            var config = Config.Load(Config.DefaultConfigPath);

            if (arguments.Dump)
            {
                logger.Info("Launching in dump mode");
                DeviceManager.DumpIconsLoop();
            }

            if (arguments.Save)
            {
                logger.Info("Saving default preset into file");
                Preset.Default.Save();
            }

            Preset preset = Preset.Default;
            if (string.IsNullOrEmpty(arguments.Preset))
                logger.Info("No preset selected. Using default");
            else
            {
                logger.Info("Trying to load preset \"{0}\"", arguments.Preset);
                preset = Preset.Load(arguments.Preset);
            }

            logger.Info("Ready to start fishing with selected preset: {0}", preset);
            if (arguments.Classic)
                logger.Info("Classic Bot will be used");
            else
                logger.Info("Retail Bot will be used");
            logger.Info("Press \"Enter\" to start");
            Console.ReadLine();
            IBot fishman;
            if (arguments.Classic)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; // due to screen capture usage
                fishman = new ClassicBot(preset, config.ClassicBotOptions);
            }
            else
                fishman = new RetailBot(preset, config.RetailBotOptions);
            fishman.Start();

            while (true);
        }
    }
}
