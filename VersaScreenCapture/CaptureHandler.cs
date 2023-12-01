using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using Device = SharpDX.Direct3D11.Device;
using Composition.WindowsRuntimeHelpers;
using System.Linq;
using Windows.Foundation.Metadata;

namespace VersaScreenCapture
{
    /// <summary>
    /// https://github.com/TheBlackPlague/DynoSharp
    /// </summary>
    public static class CaptureHandler
    {
        private static Direct3D11CaptureFramePool CaptureFramePool = null;
        private static GraphicsCaptureItem CaptureItem = null;
        private static GraphicsCaptureSession CaptureSession = null;

        private static readonly Device CaptureDevice = new Device(DriverType.Hardware, DeviceCreationFlags.BgraSupport);

        public static bool FrameCaptured { get; private set; }
        public static bool IsCapturing { get; private set; }

        public static Device GraphicCaptureDevice()
        {
            return CaptureDevice;
        }

        public static bool IsAvailable()
        {
            if (!ApiInformation.IsTypePresent("Windows.Graphics.Capture.GraphicsCaptureItem"))
            {
                Console.WriteLine("Using Windows.Graphics.Capture not possible in current system!");
                return false;
            }
            else if (!GraphicsCaptureSession.IsSupported())
            {
                Console.WriteLine("GraphicsCaptureSession not supported in current system!");
                return false;
            }

            return true;
        }

        public static void Stop()
        {
            CaptureSession.Dispose();
            CaptureFramePool.Dispose();
            CaptureSession = null;
            CaptureFramePool = null;
            CaptureItem = null;
            IsCapturing = false;
        }

        private static void StartCapture(GraphicsCaptureItem capture)
        {
            CaptureItem = capture;
            CaptureItem.Closed += CaptureItemOnClosed;

            IDirect3DDevice windowsRuntimeDevice = Direct3D11Helper.CreateDirect3DDeviceFromSharpDXDevice(CaptureDevice);


            CaptureFramePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
                windowsRuntimeDevice,
                DirectXPixelFormat.B8G8R8A8UIntNormalized,
                1, // total frames in frame pool
                CaptureItem.Size // size of each frame
                );

            CaptureSession = CaptureFramePool.CreateCaptureSession(CaptureItem);
            CaptureSession.IsCursorCaptureEnabled = true;

            //if (ApiInformation.IsPropertyPresent(typeof(GraphicsCaptureSession).FullName, nameof(GraphicsCaptureSession.IsBorderRequired)))
            //{
            //    Console.WriteLine("Turning border off.");
            //    CaptureSession.IsBorderRequired = false;
            //}

            //AppCapabilityAccessStatus appCapabilityAccessStatus = await GraphicsCaptureAccess.RequestAccessAsync(GraphicsCaptureAccessKind.Borderless);
            //if (appCapabilityAccessStatus == AppCapabilityAccessStatus.Allowed)
            //    CaptureSession.IsBorderRequired = false;

            CaptureFramePool.FrameArrived += (sender, arguments) =>
            {
                if (!IsCapturing)
                    throw new Exception("Failed to get frame without capturing!");

                AddFrame(sender.TryGetNextFrame());
            };

            CaptureSession.StartCapture();
            IsCapturing = true;
        }

        public static void StartWindowCapture(IntPtr windowHandle)
        {
            StartCapture(CaptureHelper.CreateItemForWindow(windowHandle));
        }

        public static void StartMonitorCapture(IntPtr hmon)
        {
            StartCapture(CaptureHelper.CreateItemForMonitor(hmon));
        }

        public static void StartPrimaryMonitorCapture()
        {
            MonitorInfo monitor = (from m in MonitorEnumerationHelper.GetMonitors()
                                   where m.IsPrimary
                                   select m).First();
            StartMonitorCapture(monitor.Hmon);
        }

        private static void AddFrame(Direct3D11CaptureFrame direct3D11CaptureFrame)
        {
            FramePool.FreeRuntimeResources();
            FramePool.SetLatestFrame(direct3D11CaptureFrame);
            FrameCaptured = true;
        }

        private static void CaptureItemOnClosed(GraphicsCaptureItem sender, object eventArgs)
        {
            Stop();
        }
    }
}
