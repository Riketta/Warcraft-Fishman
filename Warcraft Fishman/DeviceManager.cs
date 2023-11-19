﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fishman
{
    class DeviceManager
    {
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private static Random random = new Random();

        public static readonly string IconFileDefault = "default.bmp";
        public static readonly string IconFileFishhook = "fishhook.bmp";
        public static readonly string IconFileFishhookClassic = "fishhook_classic.bmp";

        public static Bitmap IconDefault;
        public static Bitmap IconFishhook;
        public static Bitmap IconFishhookClassic;

        public static void LoadCursors()
        {
            if (File.Exists(IconFileDefault))
                IconDefault = Image.FromFile(IconFileDefault) as Bitmap;
            else
                logger.Warn("Icon \"{0}\" not found", IconFileDefault);

            if (File.Exists(IconFileFishhook))
                IconFishhook = Image.FromFile(IconFileFishhook) as Bitmap;
            else
                logger.Warn("Icon \"{0}\" not found", IconFileFishhook);

            if (File.Exists(IconFileFishhookClassic))
                IconFishhookClassic = Image.FromFile(IconFileFishhookClassic) as Bitmap;
            else
                logger.Warn("Icon \"{0}\" not found", IconFileFishhookClassic);
        }

        public static Bitmap GetCurrentIcon()
        {
            Bitmap cursorIcon = null;

            WinApi.CURSORINFO pci;
            pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WinApi.CURSORINFO));

            if (WinApi.GetCursorInfo(out pci))
            {
                using (var icon = new Bitmap(32, 32))
                {
                    using (Graphics g = Graphics.FromImage(icon))
                        WinApi.DrawIcon(g.GetHdc(), 0, 0, pci.hCursor);
                    cursorIcon = new Bitmap(icon).Clone() as Bitmap; // We have to "clone" icon because handle won't be released if we continue use it
                }
            }

            return cursorIcon;
        }

        public static bool CompareIcons(Bitmap a, Bitmap b)
        {
            if ((a == null && b != null) || (a != null && b == null))
                return false;

            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                {
                    //a.Save("a.ico", System.Drawing.Imaging.ImageFormat.Icon);
                    //b.Save("b.ico", System.Drawing.Imaging.ImageFormat.Icon);
                    if (a.GetPixel(x, y) != b.GetPixel(x, y))
                        return false;
                }
            return true;
        }

        public static void DumpIconsLoop()
        {
            logger.Info("Infinity loop that saves current icon once per second");
            logger.Info("Save default WoW icon as " + IconFileDefault);
            logger.Info("Save fish-hook icon as " + IconFileFishhook);

            int counter = 0;
            while (true)
            {
                logger.Info("Iteration: " + counter);
                GetCurrentIcon().Save(counter.ToString() + ".bmp");
                counter++;
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Simulation of pressed key.
        /// </summary>
        /// <param name="hWnd">Window handle to send key to</param>
        /// <param name="key">Key code</param>
        public static void PressKey(IntPtr hWnd, WinApi.VirtualKeys key)
        {
            WinApi.PostMessage(hWnd, WinApi.WM_KEYDOWN, (UInt32)key, IntPtr.Zero);
            Thread.Sleep(50 + random.Next(-10, 35));
            WinApi.PostMessage(hWnd, WinApi.WM_KEYUP, (UInt32)key, IntPtr.Zero);
        }

        /// <summary>
        /// Simulate left or right mouse button click
        /// </summary>
        /// <param name="hWnd">Window handle to send key to</param>
        /// <param name="invert">Flag to use RMB instead of LMB</param>
        public static void MouseClick(IntPtr hWnd, bool invert = false)
        {
            WinApi.mouse_event((invert ? WinApi.MOUSEEVENTF_RIGHTDOWN : WinApi.MOUSEEVENTF_LEFTDOWN), 0, 0, 0, UIntPtr.Zero);
            Thread.Sleep(70 + random.Next(-12, 12));
            WinApi.mouse_event((invert ? WinApi.MOUSEEVENTF_RIGHTUP : WinApi.MOUSEEVENTF_LEFTUP), 0, 0, 0, UIntPtr.Zero);
        }

        public static Point GetMousePosition()
        {
            return System.Windows.Forms.Cursor.Position;
        }

        public static void MoveMouse(Point position)
        {
            System.Windows.Forms.Cursor.Position = position;
        }

        static public Color GetPixelColor(IntPtr hwnd, int x, int y)
        {
            IntPtr hdc = WinApi.GetWindowDC(hwnd);
            uint pixel = WinApi.GetPixel(hdc, x, y);
            WinApi.ReleaseDC(hwnd, hdc);
            Color color = Color.FromArgb((int)(pixel & 0x000000FF),
                            (int)(pixel & 0x0000FF00) >> 8,
                            (int)(pixel & 0x00FF0000) >> 16);
            return color;
        }
    }
}
