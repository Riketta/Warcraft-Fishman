using System;
using System.Collections.Generic;
using System.Drawing;
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

        public static Bitmap IconDefault;
        public static Bitmap IconFishhook;

        public static void LoadCursors()
        {
            IconDefault = Image.FromFile(IconFileDefault) as Bitmap;
            IconFishhook = Image.FromFile(IconFileFishhook) as Bitmap;
        }

        public static Bitmap GetCurrentIcon()
        {
            WinApi.CURSORINFO pci;
            pci.cbSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(WinApi.CURSORINFO));

            Bitmap cursor = new Bitmap(32, 32);
            if (WinApi.GetCursorInfo(out pci))
                using (Graphics g = Graphics.FromImage(cursor))
                    WinApi.DrawIcon(g.GetHdc(), 0, 0, pci.hCursor);

            return cursor;
        }

        public static bool CompareIcons(Bitmap a, Bitmap b)
        {
            for (int x = 0; x < 5; x++)
                for (int y = 0; y < 5; y++)
                    if (a.GetPixel(x, y) != b.GetPixel(x, y))
                        return false;
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
        /// Simulate left mouse button click
        /// </summary>
        /// <param name="hWnd">Window handle to send key to</param>
        public static void MouseClickLMB(IntPtr hWnd)
        {
            WinApi.PostMessage(hWnd, WinApi.WM_LBUTTONDOWN, (UInt32)WinApi.VirtualKeys.LeftButton, IntPtr.Zero);
            Thread.Sleep(50 + random.Next(-10, 10));
            WinApi.PostMessage(hWnd, WinApi.WM_LBUTTONUP, (UInt32)WinApi.VirtualKeys.LeftButton, IntPtr.Zero);
        }

        public static Point GetMousePosition()
        {
            return System.Windows.Forms.Cursor.Position;
        }

        public static void MoveMouse(Point position)
        {
            System.Windows.Forms.Cursor.Position = position;

        }

    }
}
