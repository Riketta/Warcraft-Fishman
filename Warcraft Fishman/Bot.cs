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
    class Bot
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        IntPtr handle = IntPtr.Zero;
        Preset preset = null;

        const int ScanningRetries = 3;
        const int ScanningSteps = 10;
        const int ScanningDelay = 16; // 1000 / ScanningDelay = Minimum required FPS

        public bool UseOffset = false;
        public bool InvertClicks = false;
        const int BobberHorizontalOffset = 12; // used to reduce fails caused by default bobber feathers

        /// <summary>
        /// Fishman bot main class
        /// </summary>
        /// <param name="preset">Actions preset that should be used for fishing</param>
        public Bot(Preset preset)
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
            logger.Info("### Fishing loop started ###");
            logger.Info("Looking for WoW window handle");
            logger.Debug("Handle: {0}", handle);
            SetGameWindowActive();

            if (!preset.Validate())
            {
                logger.Error("Invalid preset!");
                throw new Exception("Invalid preset");
            }

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

            processes = Process.GetProcessesByName("WowClassic");
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
            DeviceManager.MoveMouse(new Point(bobber.X + (bobber.Width / 2), bobber.Bottom + 4));

            if (!WaitForBite(fishing.CastTime))
                return false;

            logger.Info("Waiting bobber to stop");
            //Thread.Sleep(200);
            DeviceManager.MoveMouse(new Point(bobber.X + (bobber.Width / 2), bobber.Bottom - 15));
            Thread.Sleep(50);
            logger.Info("Mouse click");
            DeviceManager.MouseClick(handle, InvertClicks);
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
                        if (DeviceManager.CompareIcons(icon, DeviceManager.IconFishhook))
                            return true;
                    }

            logger.Warn("Bobber not found!");
            return false;
        }

        Rectangle GetBobberSize()
        {
            const int step = 4;

            Point pos = DeviceManager.GetMousePosition();
            Rectangle result = new Rectangle(pos, new Size(0, 0));


            #region Bounding Box Left Side
            pos.X -= step;
            DeviceManager.MoveMouse(pos);
            Thread.Sleep(ScanningDelay);

            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhook))
            {
                pos.X -= step;
                DeviceManager.MoveMouse(pos);
                Thread.Sleep(ScanningDelay);
            }

            // -- undo last step since is not on bobber anymore
            pos.X += step;

            // -- resize/reposition result to account for known bobber size
            result.Width = result.X - pos.X;
            result.X = pos.X;
            #endregion

            #region Bounding Box Right Side
            pos.X = result.Right + step;
            DeviceManager.MoveMouse(pos);
            Thread.Sleep(ScanningDelay);

            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhook))
            {
                pos.X += step;
                DeviceManager.MoveMouse(pos);
                Thread.Sleep(ScanningDelay);
            }
            result.Width = pos.X - result.X;
            #endregion


            // ### move to horizontal center of bounding box ###
            pos.X = result.X + (result.Width / 2) + (UseOffset ? BobberHorizontalOffset : 0);

            #region Bounding Box Bottom
            pos.Y = result.Bottom + step;
            DeviceManager.MoveMouse(pos);
            Thread.Sleep(ScanningDelay);

            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhook))
            {
                pos.Y += step;
                DeviceManager.MoveMouse(pos);
                Thread.Sleep(ScanningDelay);
            }
            result.Height = pos.Y - result.Y;
            #endregion

            return result;
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
                while (true)
                {
                    if (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), DeviceManager.IconFishhook))
                        return true;

                    Thread.Sleep(30);
                }
            });

            if (!task.Wait(timeout))
            {
                logger.Error("Bite wasn't detected: timeout occured");
                return false;
            }

            return task.Result;
        }
        #endregion
    }
}
