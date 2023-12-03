using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishman
{
    internal class RetailBotOptions : IBotOptions
    {
        public string PathToFishhookCursor { get; set; } = "fishhook_retail.bmp";

        public bool SwapMouseButtonClicks { get; set; } = false;
        public bool UseHorCursorOffset { get; set; } = false;
        public int BobberHorizontalOffset { get; set; } = 12;

        public int ScanningRetries { get; set; } = 3;
        public int ScanningSteps { get; set; } = 10;
        public int ScanningDelay { get; set; } = 16;

        public int ScanRegionXMin { get; set; } = 720;
        public int ScanRegionXMax { get; set; } = 1200;
        public int ScanRegionYMin { get; set; } = 350;
        public int ScanRegionYMax { get; set; } = 600;

        public int FishingAttemptsPerIteration { get; set; } = 5;

        #region Retail Specific Options
        #endregion
    }
}
