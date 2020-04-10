using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Fishman
{
    class ClassicBot
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        IntPtr handle = IntPtr.Zero;
        Preset preset = null;

        const int ScanningRetries = 4;
        const int ScanningSteps = 10;
        const int ScanningDelay = 20; // 1000 / ScanningDelay = Minimum required FPS

        public bool UseOffset = false;
        const int BobberHorizontalOffset = 12; // used to reduce fails caused by default bobber feathers

        /// <summary>
        /// Fishman bot main class
        /// </summary>
        /// <param name="preset">Actions preset that should be used for fishing</param>
        public ClassicBot(Preset preset)
        {
            handle = GetWoWProcess().MainWindowHandle;
            this.preset = preset;
        }

        /// <summary>
        /// Main fishing loop.
        /// Supposed that one iteration can't lasts longer than ~30 seconds: full fishing time + some other events with cast time.
        /// </summary>
        public void FishingLoop()
        {
            logger.Info("### Classic Fishing loop started ###");
            logger.Info("Looking for WoW window handle");
            logger.Debug("Handle: {0}", handle);
            SetGameWindowActive();

            if (!preset.Validate())
            {
                logger.Error("Invalid preset!");
                throw new Exception("Invalid preset");
            }

            Action.Fish.CastTime = 60 * 1000;
            logger.Info("Invoking once-only prefishing actions");
            foreach (var action in preset.GetActions(Action.Event.Once))
                action.Invoke(handle);

            while (true)
            {
                logger.Info("### Fishing iteration started ###");

                logger.Info("Invoking pre-fishing actions");
                foreach (var action in preset.GetActions(Action.Event.PreFish))
                    action.Invoke(handle);

                logger.Info("Checking events");
                foreach (var action in preset.GetActions(Action.Event.Interval))
                    action.Invoke(handle);

                logger.Info("Starting fishing");
                try
                {
                    bool success = false;
                    while (!success)
                    {
                        DateTime fishingStarted = DateTime.Now;
                        success = Fishing(preset.GetActions(Action.Event.Fish)[0]);
                        Statistics.AddTry(DateTime.Now.Subtract(fishingStarted).TotalSeconds, success);

                        if (!success)
                            logger.Warn("Starting fishing again");
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Exception occured while fishing: " + ex.ToString());
                }

                logger.Info("Invoking post-fishing actions");
                foreach (var action in preset.GetActions(Action.Event.PostFish))
                    action.Invoke(handle);
            }
        }

        public void SetGameWindowActive()
        {
            bool SyncShow = WinApi.SetForegroundWindow(handle);
            bool ASyncShow = WinApi.ShowWindowAsync(handle, 9); // SW_RESTORE = 9
            Thread.Sleep(300);
        }

        private static Process GetWoWProcess()
        {
            Process wow = null;

            Process[] processes = Process.GetProcessesByName("Wow-64");
            if (processes.Length > 0)
                wow = processes[0];

            processes = Process.GetProcessesByName("Wow");
            if (processes.Length > 0)
                wow = processes[0];

            return wow;
        }

        #region Fishing actions
        public bool Fishing(Action fishing)
        {
            fishing.Invoke(handle);

            if (!FindBobber())
                return false;

            Rectangle bobber = GetBobberSize();
            DeviceManager.MoveMouse(new Point(bobber.Right - (int)(bobber.Width * 0.20f), bobber.Top + (int)(bobber.Height * 0.275f)));

            if (!WaitForBite(fishing.CastTime))
                return false;

            DeviceManager.MoveMouse(new Point(bobber.X + (bobber.Width / 2), bobber.Bottom - 15));
            logger.Info("Mouse click");
            DeviceManager.MouseClick(handle);
            logger.Info("Loot delay");
            Thread.Sleep(1000);

            return true;
        }

        /// <summary>
        /// Bobber searching loop. Prepares cursor for bite waiting loop
        /// Use only with <see cref="Action.Event.Fish"/>
        /// </summary>
        /// <returns>true if bobber found, false if else</returns>
        bool FindBobber()
        {
            logger.Debug("Looking for a bobber");

            Screen screen = Screen.PrimaryScreen;
            Point pos = new Point();

            int xMin = screen.Bounds.Width / 3;
            int xMax = xMin * 2;
            int yMin = screen.Bounds.Height / 4;
            int yMax = yMin * 2;

            int xStep = ((xMax - xMin) / ScanningSteps);
            int yStep = ((yMax - yMin) / ScanningSteps);
            int xOffSet = (xStep / ScanningRetries);

            for (int ScanAttempt = 0; ScanAttempt <= ScanningRetries; ScanAttempt++)
                for (int mouseX = xMin + xOffSet * ScanAttempt; mouseX < xMax; mouseX += xStep)
                    for (int mouseY = yMin; mouseY < yMax; mouseY += yStep)
                    {
                        pos.X = screen.WorkingArea.X + mouseX;
                        pos.Y = screen.WorkingArea.Y + mouseY;
                        DeviceManager.MoveMouse(pos);

                        Thread.Sleep(ScanningDelay);

                        Bitmap icon = DeviceManager.GetCurrentIcon();
                        if (DeviceManager.CompareIcons(icon, DeviceManager.IconFishhookClassic))
                            return true;
                    }

            logger.Warn("Bobber not found!");
            return false;
        }

        Rectangle GetBobberSize()
        {
            const int step = 1;

            Point cursor = DeviceManager.GetMousePosition();
            Rectangle bobber = new Rectangle(0, 0, 0, 0);

            // HORIZONTAL
            cursor.Y += 3 * step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(ScanningDelay);

            #region Left Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhookClassic))
            {
                cursor.X -= step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(ScanningDelay);
            }
            cursor.X += step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(ScanningDelay);
            #endregion
            bobber.X = cursor.X;
            
            #region Right Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhookClassic))
            {
                cursor.X += step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(ScanningDelay);
            }
            cursor.X -= step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(ScanningDelay);
            #endregion
            bobber.Width = cursor.X - bobber.X;

            //  VERTICAL

            cursor.X = bobber.X + bobber.Width / 2;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(ScanningDelay);

            #region Top Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhookClassic))
            {
                cursor.Y -= step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(ScanningDelay);
            }
            cursor.Y += step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(ScanningDelay);
            #endregion
            bobber.Y = cursor.Y;

            #region Bottom Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhookClassic))
            {
                cursor.Y += step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(ScanningDelay);
            }
            cursor.Y -= step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(ScanningDelay);
            #endregion
            bobber.Height = cursor.Y - bobber.Y;

            return bobber;
        }

        /// <summary>
        /// Loop that should be called after <see cref="FindBobber"/> with true result.
        /// Waits for bite. Waits until bite or until timeout.
        /// Use only with <see cref="Action.Event.Fish"/>.
        /// </summary>
        /// <param name="timeout">Timeout in milliseconds</param>
        /// <returns>true if bite detected</returns>
        bool WaitForBite(int timeout)
        {
            logger.Debug("Waiting for bite");

            var task = Task.Run(() =>
            {
                logger.Debug("Fixating color");
                Color color = FixateColor();
                logger.Debug("[{0}, {1}, {2}]", color.R, color.G, color.B);

                int changedCount = 0;
                DateTime dateTime = DateTime.MinValue;
                logger.Debug("Bite loop");
                while (true)
                {
                    Point mousePosition = DeviceManager.GetMousePosition();
                    Color mouseColor = DeviceManager.GetPixelColor(handle, mousePosition.X, mousePosition.Y);

                    if (IsColorChanged(color, mouseColor))
                    {
                        logger.Debug("Changed: [{0}, {1}, {2}]", mouseColor.R, mouseColor.G, mouseColor.B);

                        if (dateTime == DateTime.MinValue)
                            dateTime = DateTime.UtcNow;
                        else if (DateTime.UtcNow.Subtract(dateTime).TotalMilliseconds > 150) // if we catched new series of true results - reset old data
                        {
                            dateTime = DateTime.UtcNow;
                            changedCount = 0;
                        }

                        changedCount++;
                    }

                    if (changedCount == 3)
                        return true;

                    Thread.Sleep(25);
                }
            });

            if (!task.Wait(timeout))
            {
                logger.Error("Bite wasn't detected: timeout occured");
                return false;
            }

            return task.Result;
        }

        int fixateMaxTicks = 120;
        int fixateInterval = 25;
        /// <summary>
        /// Calculating "average" background color under cursor over some time (check <see cref="fixateMaxTicks"/> and <see cref="fixateInterval"/>).
        /// </summary>
        /// <returns><see cref="Color"/> as average background color</returns>
        Color FixateColor()
        {
            Color color = Color.Empty;
            float r = 0;
            float g = 0;
            float b = 0;

            int ticks = 0;
            while (true)
            {
                Point position = DeviceManager.GetMousePosition();
                color = DeviceManager.GetPixelColor(handle, position.X, position.Y);
                r += color.R;
                g += color.G;
                b += color.B;

                ticks++;
                if (ticks >= fixateMaxTicks)
                    break;
                Thread.Sleep(fixateInterval);
            }

            color = Color.FromArgb(0, (byte)(r / ticks), (byte)(g / ticks), (byte)(b / ticks));
            return color;
        }

        float colorError = 0.15f;
        /// <summary>
        /// Calculating difference between two colors in range of % error.
        /// </summary>
        /// <returns>true if difference out of <see cref="colorError"/> bound</returns>
        bool IsColorChanged(Color main, Color secondary)
        {
            float diffR = (float)(main.R - secondary.R) / main.R;
            float diffG = (float)(main.G - secondary.G) / main.G;
            float diffB = (float)(main.B - secondary.B) / main.B;
            float diffAvg = (diffR + diffG + diffB) / 3;

            //logger.Debug("Diff: {0}", diffAvg);

            if (Math.Abs(diffAvg) >= colorError)
                return true;

            return false;
        }
        #endregion
    }
}
