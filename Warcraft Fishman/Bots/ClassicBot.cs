﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using VersaScreenCapture;

namespace Fishman
{
    class ClassicBot : IBot
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        protected override IntPtr Handle { get; set; } = IntPtr.Zero;
        protected override Preset Preset { get; set; } = null;
        protected override Bitmap FishhookCursor { get; set; }

        readonly ClassicBotOptions _options = null;
        readonly FrameProcessor _frameProcessor = null;

        bool _lastFrameDetectionState = false;

        /// <summary>
        /// Classic and TBC version of the bot.
        /// Strategy:
        /// 1. Scan the screen with the cursor in search of the bobber.
        /// 2. Once the bobber is found, scan its boundaries and measure the size.
        /// 3. Place the cursor in the center of the bobber.
        /// 4. TODO: Scale the expected template size with the actual bobber size. For example: the further the bobber is from the player, the smaller the region must be to match the template (template should scale down too).
        /// 5. Run pattern matching using OpenCV and match the target region (a rectangle at an offset from the cursor) with a pattern (e.g. red feather).
        /// 6. If the pattern matches (or no longer matches if the condition is inverted) - a bite has occurred.
        /// 7. Click on the bobber with the mouse and wait the loot.
        /// Tips:
        /// - Higher (or sometimes lower) quality water graphics can help increase the success rate of fishing attempts.
        /// - The higher your character is, the more to the right the fishing-line will be (so less overlap with the bobber).
        /// - Reducing the FPS on weak systems (e.g. with an integrated GPU) to 60-80 may help, and also set the water quality to "Fair".
        /// - Increase "Contrast" to maximum.
        /// </summary>
        /// <param name="preset">Actions preset that should be used for fishing.</param>
        /// <param name="options">Set of options for this bot version.</param>
        public ClassicBot(Preset preset, ClassicBotOptions options)
        {
            Handle = GetWoWProcess().MainWindowHandle;
            Preset = preset;
            _options = options;

            logger.Info("Trying to load cursor a image from a file");
            FishhookCursor = DeviceManager.LoadCursor(_options.PathToFishhookCursor);
            if (FishhookCursor is null)
                throw new Exception($"Cursor icon \"{_options.PathToFishhookCursor}\" not found!");

            _frameProcessor = new FrameProcessor(_options.PathToTemplate)
            {
                Debug = _options.DebugOpenCV
            };
        }

        ~ClassicBot()
        {
            Stop();
        }

        public override void Start()
        {
            logger.Info("### Classic Fishing loop started ###");
            logger.Info("Looking for WoW window handle");
            logger.Debug("Handle: {0}", Handle);
            SetGameWindowActive();

            if (!Preset.Validate())
            {
                logger.Error("Invalid preset!");
                throw new Exception("Invalid preset");
            }
            CaptureHandler.StartWindowCapture(Handle);

            //Action.Fish.CastTime = 60 * 1000; // classic override, tune in preset

            FishingLoop();
        }

        public override void Stop()
        {
            CaptureHandler.Stop();
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

        public void SetGameWindowActive()
        {
            bool SyncShow = Win32.SetForegroundWindow(Handle);
            bool ASyncShow = Win32.ShowWindowAsync(Handle, 9); // SW_RESTORE = 9
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
        protected override bool Fishing(Action fishing)
        {
            fishing.Invoke(Handle);

            if (!FindBobber())
                return false;

            Rectangle bobber = GetBobberSize();
            DeviceManager.MoveMouse(new Point(bobber.Right - (int)(bobber.Width * 0.25f), bobber.Top + (int)(bobber.Height * 0.5f)));

            if (!WaitForBite(fishing.CastTime))
                return false;

            //Thread.Sleep(200);
            //DeviceManager.MoveMouse(new Point(bobber.X + (bobber.Width / 2), bobber.Bottom - 15));
            logger.Info("Mouse click");
            DeviceManager.MouseClick(Handle, _options.SwapMouseButtonClicks);
            logger.Info("Loot delay");
            Thread.Sleep(350);

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
            const int step = 5;

            Point cursor = DeviceManager.GetMousePosition();
            Rectangle bobber = new Rectangle(0, 0, 0, 0);

            // HORIZONTAL
            cursor.Y += 3 * step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(_options.ScanningDelay);

            #region Left Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                cursor.X -= step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(_options.ScanningDelay);
            }
            cursor.X += step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(_options.ScanningDelay);
            #endregion
            bobber.X = cursor.X;

            #region Right Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                cursor.X += step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(_options.ScanningDelay);
            }
            cursor.X -= step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(_options.ScanningDelay);
            #endregion
            bobber.Width = cursor.X - bobber.X;

            //  VERTICAL
            cursor.X = bobber.X + bobber.Width / 2;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(_options.ScanningDelay);

            #region Top Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                cursor.Y -= step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(_options.ScanningDelay);
            }
            cursor.Y += step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(_options.ScanningDelay);
            #endregion
            bobber.Y = cursor.Y;

            #region Bottom Bound
            while (DeviceManager.CompareIcons(DeviceManager.GetCurrentIcon(), FishhookCursor))
            {
                cursor.Y += step;
                DeviceManager.MoveMouse(cursor);
                Thread.Sleep(_options.ScanningDelay);
            }
            cursor.Y -= step;
            DeviceManager.MoveMouse(cursor);
            Thread.Sleep(_options.ScanningDelay);
            #endregion
            bobber.Height = cursor.Y - bobber.Y;

            return bobber;
        }

        protected override bool WaitForBite(int timeout)
        {
            logger.Debug("Waiting for bite");

            Point mousePosition = DeviceManager.GetMousePosition();
            Rectangle detectionRegion = new Rectangle(mousePosition.X + _options.DetectionRegion.X,
                                                      mousePosition.Y + _options.DetectionRegion.Y,
                                                      _options.DetectionRegion.Width,
                                                      _options.DetectionRegion.Height);
            //logger.Debug("{0}x{1}", mousePosition.X, mousePosition.Y);

            var cancellationTokenSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancellationTokenSource.Token;

            double minValue = 1;
            double maxValue = -1;
            List<double> values = null;
            if (_options.DebugWeight)
                values = new List<double>(2000);

            var task = Task.Run(() =>
            {
                logger.Debug("Bite loop");

                double value = 0;
                while (true)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        logger.Warn("[-] Cancelling a WaitForBite task.");
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    value = _frameProcessor.BiteDetection(detectionRegion);
                    if (value == 0)
                        continue;

                    if (_options.DebugWeight || _options.DebugCSV)
                        values.Add(value);
                    if (value < minValue)
                        minValue = value;
                    else if (value > maxValue)
                        maxValue = value;

                    if ((value > _options.MinThreshold * 0.7 && value < _options.MaxThreshold * 1.5) || _options.DebugOpenCV || _options.DebugWeight)
                        logger.Debug($"Weight: {value:F3} [{_options.MinThreshold}/{_options.MaxThreshold}]; Prev frame was detected: {_lastFrameDetectionState}");

                    if (value > _options.MinThreshold && value < _options.MaxThreshold)
                    {
                        if (_lastFrameDetectionState) // two frame validation, 2/2 frames in a row with a match found
                        {
                            _lastFrameDetectionState = false;
                            logger.Debug($"Match: {value:F3} [{_options.MinThreshold}/{_options.MaxThreshold}]");

                            return true;
                        }
                        else // the first frame (of two required ones) with the correct match was found
                        {
                            _lastFrameDetectionState = true;
                            continue;
                        }
                    }
                    else
                        _lastFrameDetectionState = false;

                    //Thread.Sleep(1);
                }
            }, cancellationToken);

            bool isTimeout = !task.Wait(timeout);
            if (isTimeout)
            {
                cancellationTokenSource.Cancel();
                logger.Error("Bite wasn't detected: timeout occured");
            }

            // Save raw (not yet sorted and not modified) weight values as CSV-file.
            if (_options.DebugCSV)
            {
                string fileName = $"{DateTime.Now.Ticks}.csv";
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    writer.WriteLine("Frame,Weigh");
                    for (int i = 0; i < values.Count; i++)
                        writer.WriteLine($"{i + 1},{values[i]}");
                }
                logger.Debug($"Weight data saved as \"{fileName}\"");
            }

            logger.Debug($"Match Range (total): [{minValue:F3}; {maxValue:F3}]");
            // Calculate filtered minimum and maximum values (without bite range).
            if (_options.DebugWeight)
            {
                values.Sort();

                int startIndex = values.Count - (int)(values.Count * 0.950);
                if (startIndex < 0)
                    startIndex = 0;
                else if (startIndex >= values.Count)
                    startIndex = values.Count - 1;

                int endIndex = values.Count - 1;

                logger.Debug($"> Entries: {values.Count}; Start Index: {startIndex}; End Index: {endIndex}.");
                double minValueWithoutBite = values[startIndex];
                double maxValueWithoutBite = values[endIndex];

                logger.Debug($"Match Range (filtered): [{minValueWithoutBite:F3}; {maxValueWithoutBite:F3}]");
            }

            if (isTimeout)
                return false;

            return task.Result;
        }
        #endregion
    }
}
