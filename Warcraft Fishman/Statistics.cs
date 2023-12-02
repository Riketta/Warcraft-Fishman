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

        static double MinFishing = 0;
        static double MaxFishing = 0;

        /// <summary>
        /// The amount of time spent on successful fishing.
        /// </summary>
        static double TotalTimeFishing = 0.0;

        /// <summary>
        /// Amount of time spent on unsuccessful fishing attempts.
        /// </summary>
        static double WastedTimeFishing = 0.0;

        public static void AddTry(double timeSpent, bool isSuccess)
        {
            logger.Debug($"Time spent on last attempt: {timeSpent:F2} seconds");

            TotalTries++;
            if (isSuccess && timeSpent > 3.0) // timeSpent includes all sleeps bot do during fishing try
            {
                SuccessTries++;

                TotalTimeFishing += timeSpent;
                if (timeSpent < MinFishing || MinFishing == 0)
                    MinFishing = timeSpent;
                else if (timeSpent > MaxFishing)
                    MaxFishing = timeSpent;
            }
            else
            {
                FailedTries++;
                WastedTimeFishing += timeSpent;
            }
        }

        public static string GetReport()
        {
            double timePerSuccessfulAttempt = Math.Round(TotalTimeFishing / SuccessTries, 2);

            string triesReport = $"Total tries: {TotalTries}; Success: {SuccessTries}; Failed: {FailedTries};";
            string timeReportA = $"Average execution time: {timePerSuccessfulAttempt:F2}; Min: {Math.Round(MinFishing, 2):F2}; Max: {Math.Round(MaxFishing, 2):F2};";
            string timeReportB = $"Total time fishing: {TotalTimeFishing:F2} seconds; Wasted time: {WastedTimeFishing:F2} seconds;";
            string report = string.Format($"### Statistic Report ###{Environment.NewLine}  {triesReport}{Environment.NewLine}  {timeReportA}{Environment.NewLine}  {timeReportB}");

            return report;
        }
    }
}
