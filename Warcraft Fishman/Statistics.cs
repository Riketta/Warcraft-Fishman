using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishman
{
    class Statistics
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        static int TotalTries = 0;
        static int SuccessTries = 0; // including false-positives
        static int FailedTries = 0;

        static double MinFishing = double.MaxValue;
        static double MaxFishing = double.MinValue;
        static double TotalFishing = 0.0;

        public static void AddTry(double timeSpent, bool isSuccess)
        {
            logger.Debug("Time spent for last try: {0}", timeSpent);

            TotalTries++;
            if (isSuccess && timeSpent > 3.0) // timeSpent includes all sleeps bot do during fishing try
            {
                SuccessTries++;

                TotalFishing += timeSpent;
                if (timeSpent < MinFishing)
                    MinFishing = timeSpent;
                else if (timeSpent > MaxFishing)
                    MaxFishing = timeSpent;
            }
            else
                FailedTries++;
        }

        public static string GetReport()
        {
            string triesReport = $"Total tries: {TotalTries}; Success: {SuccessTries}; Failed: {FailedTries};";
            string timeReport = $"Average execution time: {Math.Round(TotalFishing / SuccessTries, 2)}; Min: {Math.Round(MinFishing, 2)}; Max: {Math.Round(MaxFishing, 2)}";
            string report = string.Format("### Statistic Report ###{0}{1}{0}{2}", Environment.NewLine, triesReport, timeReport);

            return report;
        }
    }
}
