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
    class RetailBot : IBot
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        protected override IntPtr Handle { get; set; } = IntPtr.Zero;
        protected override Preset Preset { get; set; } = null;
        protected override Bitmap FishhookCursor { get; set; }
        
        readonly RetailBotOptions _options = null;

        /// <summary>
        /// Retail, Wrath and Cata version of the bot.
        /// </summary>
        /// <param name="preset">Actions preset that should be used for fishing.</param>
        /// <param name="options">Set of options for this bot version.</param>
        public RetailBot(Preset preset, RetailBotOptions options)
        {
            Handle = GetWoWProcess().MainWindowHandle;
            Preset = preset;
            _options = options;

            logger.Info("Trying to load cursor a image from a file");
            FishhookCursor = DeviceManager.LoadCursor(_options.PathToFishhookCursor);
            if (FishhookCursor is null)
                throw new Exception($"Cursor icon \"{_options.PathToFishhookCursor}\" not found!");
        }

        public override void Start()
        {
            logger.Info("### Fishing loop started ###");
            logger.Info("Looking for WoW window handle");
            logger.Debug("Handle: {0}", Handle);
            SetGameWindowActive();

            if (!Preset.Validate())
            {
                logger.Error("Invalid preset!");
                throw new Exception("Invalid preset");
            }

            FishingLoop();
        }

        public override void Stop()
        {
        }

        protected override void FishingLoop()
        {
            logger.Info("Invoking once-only prefishing actions");
            foreach (var action in Preset.GetActions(Action.Event.Once))
                action.Invoke(Handle);

            while (true)
            {
                logger.Info("### Fishing iteration started ###");

                logger.Info("Invoking pre-fishing actions");
                foreach (var action in Preset.GetActions(Action.Event.PreFish))
                    action.Invoke(Handle);

                logger.Info("Checking events");
                foreach (var action in Preset.GetActions(Action.Event.Interval))
                    action.Invoke(Handle);

                logger.Info("Starting fishing");
                try
                {
                    int remainingAttempts = _options.FishingAttemptsPerIteration;
                    bool success = false;
                    while (!success && remainingAttempts > 0)
                    {
                        DateTime fishingStarted = DateTime.Now;
                        success = Fishing(Preset.GetActions(Action.Event.Fish)[0]);
                        Statistics.AddTry(DateTime.Now.Subtract(fishingStarted).TotalSeconds, success);

                        if (!success)
                        {
                            remainingAttempts--;
                            logger.Warn($"Fishing attempt failed. Remaining attempts: {remainingAttempts}.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Exception occured while fishing: " + ex.ToString());
                }

                logger.Info("Invoking post-fishing actions");
                foreach (var action in Preset.GetActions(Action.Event.PostFish))
                    action.Invoke(Handle);
            }
        }

        void SetGameWindowActive()
        {
            bool SyncShow = Win32.SetForegroundWindow(Handle);
            bool ASyncShow = Win32.ShowWindowAsync(Handle, 9); // SW_RESTORE = 9
            Thread.Sleep(300);
        }

        static Process GetWoWProcess()
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
        protected override bool Fishing(Action fishing)
        {
            fishing.Invoke(Handle);

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
            DeviceManager.MouseClick(Handle, _options.SwapMouseButtonClicks);
            logger.Info("Loot delay");
            Thread.Sleep(1000);

            return true;
        }

        protected override bool FindBobber()
        {
            logger.Debug("Looking for a bobber");

            Screen screen = Screen.PrimaryScreen;
            Point pos = new Point();

            int xMin = _options.ScanRegionXMin;
            int xMax = _options.ScanRegionXMax;
            int yMin = _options.ScanRegionYMin;
            int yMax = _options.ScanRegionYMax;

            int xStep = ((xMax - xMin) / _options.ScanningSteps);
            int yStep = ((yMax - yMin) / _options.ScanningSteps);
            int xOffSet = (xStep / _options.ScanningRetries);

            for (int ScanAttempt = 0; ScanAttempt <= _options.ScanningRetries; ScanAttempt++)
                for (int mouseX = xMin + xOffSet * ScanAttempt; mouseX < xMax; mouseX += xStep)
                    for (int mouseY = yMin; mouseY < yMax; mouseY += yStep)
                    {
                        pos.X = screen.WorkingArea.X + mouseX;
                        pos.Y = screen.WorkingArea.Y + mouseY;
                        DeviceManager.MoveMouse(pos);

                        Thread.Sleep(_options.ScanningDelay);

                        Bitmap icon = DeviceManager.GetCurrentIcon();
                        if (DeviceManager.CompareIcons(icon, FishhookCursor))
                            return true;
                    }

            logger.Warn("Bobber not found!");
            return false;
        }

        protected override Rectangle GetBobberSize()
        {
            const int step = 4;

            Point pos = DeviceManager.GetMousePosition();
            Rectangle result = new Rectangle(pos, new Size(0, 0));


            #region Bounding Box Left Side
            pos.X -= step;
            DeviceManager.MoveMouse(pos);
            Thread.Sleep(_options.ScanningDelay);

            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                pos.X -= step;
                DeviceManager.MoveMouse(pos);
                Thread.Sleep(_options.ScanningDelay);
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
            Thread.Sleep(_options.ScanningDelay);

            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                pos.X += step;
                DeviceManager.MoveMouse(pos);
                Thread.Sleep(_options.ScanningDelay);
            }
            result.Width = pos.X - result.X;
            #endregion


            // ### move to horizontal center of bounding box ###
            pos.X = result.X + (result.Width / 2) + (_options.UseHorCursorOffset ? _options.BobberHorizontalOffset : 0);

            #region Bounding Box Bottom
            pos.Y = result.Bottom + step;
            DeviceManager.MoveMouse(pos);
            Thread.Sleep(_options.ScanningDelay);

            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                pos.Y += step;
                DeviceManager.MoveMouse(pos);
                Thread.Sleep(_options.ScanningDelay);
            }
            result.Height = pos.Y - result.Y;
            #endregion

            return result;
        }

        protected override bool WaitForBite(int timeout)
        {
            logger.Debug("Waiting for bite");

            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            var task = Task.Run(() =>
            {
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.Debug("Cancelling WaitForBite Bite Loop task.");
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    if (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
                        return true;

                    Thread.Sleep(30);
                }
            }, cancellationToken);

            if (!task.Wait(timeout))
            {
                logger.Error("Bite wasn't detected: timeout occured");
                cancellationTokenSource.Cancel();

                return false;
            }

            return task.Result;
        }
        #endregion
    }
}
