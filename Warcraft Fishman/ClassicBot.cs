using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VersaScreenCapture;

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
        public bool InvertClicks = false;
        const int BobberHorizontalOffset = 12; // used to reduce fails caused by default bobber feathers

        bool lastState = false;
        double MinThreshold = 0.050; // [0.12; 0.15] for full bobber tracking | [0.05; 0.10] for red feather tracking
        double MaxThreshold = 0.095; // [0.20; 0.40] for full bobber tracking, 0.250 default | [0.08; 0.12] for red feather tracking

        /// <summary>
        /// Fishman bot main class
        /// </summary>
        /// <param name="preset">Actions preset that should be used for fishing</param>
        public ClassicBot(Preset preset)
        {
            handle = GetWoWProcess().MainWindowHandle;
            this.preset = preset;
        }

        ~ClassicBot()
        {
            CaptureHandler.Stop();
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
            CaptureHandler.StartPrimaryMonitorCapture();

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
            bool SyncShow = Win32.SetForegroundWindow(handle);
            bool ASyncShow = Win32.ShowWindowAsync(handle, 9); // SW_RESTORE = 9
            Thread.Sleep(300);
        }

        private static Process GetWoWProcess()
        {
            Process wow = null;

            Process[] processes = Process.GetProcessesByName("WowClassic");
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
            DeviceManager.MoveMouse(new Point(bobber.Right - (int)(bobber.Width * 0.25f), bobber.Top + (int)(bobber.Height * 0.5f)));

            if (!WaitForBite(fishing.CastTime))
                return false;

            //Thread.Sleep(200);
            //DeviceManager.MoveMouse(new Point(bobber.X + (bobber.Width / 2), bobber.Bottom - 15));
            logger.Info("Mouse click");
            DeviceManager.MouseClick(handle, InvertClicks);
            logger.Info("Loot delay");
            Thread.Sleep(350);

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

            int xMin = screen.Bounds.Width / 2 - (screen.Bounds.Width / 8);
            int xMax = screen.Bounds.Width / 2 + (screen.Bounds.Width / 8);
            int yMin = screen.Bounds.Height - (int)(screen.Bounds.Height / 2.25) - (int)(screen.Bounds.Height / 4.3);
            int yMax = screen.Bounds.Height - (int)(screen.Bounds.Height / 2.25);

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
            const int step = 5;

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
            bool state = false;
            
            logger.Debug("Waiting for bite");

            Point mousePosition = DeviceManager.GetMousePosition();
            Rectangle detectionRegion = new Rectangle(mousePosition.X - 62, mousePosition.Y - 20, 60, 20); // 60x18 sized rect, centred at bobber which pointed by cursor, red feather only
            //Rectangle detectionRegion = new Rectangle(mousePosition.X - 60, mousePosition.Y - 35, 80, 50); // 80x50 sized rect, centred at bobber which pointed by cursor, full bobber
            //logger.Debug("{0}x{1}", mousePosition.X, mousePosition.Y);

            var task = Task.Run(() =>
            {
                logger.Debug("Bite loop");
                
                double value = 0;
                while (true)
                {
                    value = TemplateMatching.BiteDetection(detectionRegion);
                    if (value == 0)
                        continue;

                    if (value < MaxThreshold * 1.5)
                        logger.Debug("Weight: {0}", value);

                    if (value > MinThreshold && value < MaxThreshold)
                    {
                        if (lastState) // two frame validation
                        {
                            state = true;
                            lastState = false;
                            break;
                        }
                        else
                        {
                            lastState = true;
                            continue;
                        }
                    }
                    else
                        lastState = false;

                    //Thread.Sleep(1);
                }
            });

            if (!task.Wait(timeout))
                logger.Error("Bite wasn't detected: timeout occured");

            return state;
        }
        #endregion
    }
}
