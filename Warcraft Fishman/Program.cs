using System;
using System.Collections.Generic;
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
            LogManager.SetupLogger();
            logger.Info("{0} ver. {1}", BotName, Assembly.GetEntryAssembly().GetName().Version.ToString());
            logger.Info("Author Riketta. Feedback: rowneg@bk.ru / https://github.com/riketta");

            Arguments arguments = null;
            var result = Parser.Default.ParseArguments<Arguments>(args).WithParsed(opts => arguments = opts);

            if (arguments != null && arguments.IsDump)
            {
                logger.Info("Launching in dump mode");
                DeviceManager.DumpIconsLoop();
            }

            if (arguments != null && arguments.IsSave)
            {
                logger.Info("Saving default preset into file");
                Preset.Default.Save();
            }

            logger.Info("Loading cursors");
            DeviceManager.LoadCursors();

            Preset preset = Preset.Default;
            if (arguments == null || string.IsNullOrEmpty(arguments.Preset))
                logger.Info("No preset selected. Using default");
            else
            {
                logger.Info("Trying to load preset \"{0}\"", arguments.Preset);
                preset = Preset.Load(arguments.Preset);
            }

            logger.Info("Using bobber offset: {0}", arguments.Offset);
            
            logger.Info("Ready to start fishing with selected preset: {0}", preset);
            logger.Info("Press \"Enter\" to start");
            Console.ReadLine();
            Bot fishman = new Bot(preset);
            fishman.UseOffset = arguments.Offset;
            fishman.FishingLoop();

            while (true);
        }

    }
}
