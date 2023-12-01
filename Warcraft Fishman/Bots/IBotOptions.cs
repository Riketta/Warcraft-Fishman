using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fishman
{
    interface IBotOptions
    {
        /// <summary>
        /// The path to the cursor image that is displayed when the cursor is on the bobber. Must be dumped using the --dump flag.
        /// </summary>
        string PathToFishhookCursor { get; set; }

        /// <summary>
        /// If true, instead of LMB, RMB is used to click on the bobber.
        /// </summary>
        bool SwapMouseButtonClicks { get; set; }

        /// <summary>
        /// Used to reduce the number of failures caused by standard bobber feathers.
        /// Since the feathers of a standard bobber protrude strongly to the left, this can significantly distort the measured dimensions of the bobber.
        /// Best way to solve this problem is to use custom skin (e.g.: Yellow Duck or Cat).
        /// </summary>
        bool UseHorCursorOffset { get; set; }

        /// <summary>
        /// This offset (in pixels) will be added to the final horizontal position (during bite wait) of the cursor to help solve this problem a bit if <see cref="UseHorCursorOffset"/> set to true.
        /// </summary>
        int BobberHorizontalOffset { get; set; }

        /// <summary>
        /// 
        /// </summary>
        int ScanningRetries { get; set; }
        
        /// <summary>
        /// 
        /// </summary>
        int ScanningSteps { get; set; }

        /// <summary>
        /// 1000 ms / ScanningDelay = Minimum required FPS
        /// </summary>
        int ScanningDelay { get; set; }

        /// <summary>
        /// For FullHD: 720 pixels. Value = (Window.Width / 2) - (Window.Width / 8).
        /// </summary>
        int ScanRegionXMin { get; set; }

        /// <summary>
        /// For FullHD: 1200 pixels. Value = (Window.Width / 2) + (Window.Width / 8).
        /// </summary>
        int ScanRegionXMax { get; set; }

        /// <summary>
        /// For FullHD: 350 pixels. Value = Window.Height - (Window.Height / 2.25) - (Window.Height / 4.3) = <see cref="ScanRegionYMax"/> - (Window.Height / 4.3).
        /// </summary>
        int ScanRegionYMin { get; set; }

        /// <summary>
        /// For FullHD: 600 pixels. Value = Window.Height - (Window.Height / 2.25).
        /// </summary>
        int ScanRegionYMax { get; set; }
    }
}
