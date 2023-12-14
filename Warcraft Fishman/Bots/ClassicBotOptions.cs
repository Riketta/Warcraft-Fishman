using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fishman
{
    internal class ClassicBotOptions : IBotOptions
    {
        public string PathToFishhookCursor { get; set; } = "fishhook_classic.bmp";

        public bool SwapMouseButtonClicks { get; set; } = false;
        public bool UseHorCursorOffset { get; set; } = false;
        public int BobberHorizontalOffset { get; set; } = 12;

        public int ScanningRetries { get; set; } = 4;
        public int ScanningSteps { get; set; } = 10;
        public int ScanningDelay { get; set; } = 20;

        public int ScanRegionXMin { get; set; } = 720;
        public int ScanRegionXMax { get; set; } = 1200;
        public int ScanRegionYMin { get; set; } = 350;
        public int ScanRegionYMax { get; set; } = 600;

        public int FishingAttemptsPerIteration { get; set; } = 5;

        #region Classic Specific Options
        public bool DebugOpenCV { get; set; } = false;
        public bool DebugWeight { get; set; } = false;

        /// <summary>
        /// Path to template image file.
        /// </summary>
        public string PathToTemplate { get; set; } = "template_bobber_classic.png";

        /// <summary>
        /// Red feather tracking.
        /// Idle: [0.380; 0.700].
        /// Bite: [-0.250; 0.150]. On bad graphics [-0.250; 0.250] for a bite.
        /// </summary>
        public double MinThreshold { get; set; } = -0.25;

        /// <summary>
        /// See <see cref="MinThreshold"/>.
        /// </summary>
        public double MaxThreshold { get; set; } = 0.15;

        /// <summary>
        /// The region in which template matching should work.
        /// The top left point (X and Y) must be relative to the cursor position.
        /// Should be approximately the size of the template image: <see cref="PathToTemplate"/>.
        /// </summary>
        public Rectangle DetectionRegion { get; set; } = new Rectangle(-46, -10, 44, 12);
        #endregion
    }
}
