using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Capture;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace VersaScreenCapture
{
    /// <summary>
    /// https://github.com/TheBlackPlague/DynoSharp
    /// </summary>
    public static class FramePool
    {
        private static Direct3D11CaptureFrame LatestFrame;

        public static void FreeRuntimeResources()
        {
            LatestFrame?.Dispose();
        }

        public static void SetLatestFrame(Direct3D11CaptureFrame frame)
        {
            LatestFrame = frame;
        }

        private static Direct3D11CaptureFrame GetLatestFrame()
        {
            return Interlocked.Exchange(ref LatestFrame, null);
        }

        public static Texture2D GetFrameAsTexture2D(Direct3D11CaptureFrame frame)
        {
            Device device = CaptureHandler.GraphicCaptureDevice();

            Texture2D surfaceTexture = null;
            try
            {
                surfaceTexture = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface);
            }
            catch (ObjectDisposedException ex)
            {
                //Console.WriteLine($"Exception: {ex.ObjectName} - {ex.Message}");
                return null;
            }
            frame.Dispose();

            Texture2DDescription description = new Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = BindFlags.None,
                CpuAccessFlags = CpuAccessFlags.Read,
                Format = Format.B8G8R8A8_UNorm,
                Height = surfaceTexture.Description.Height,
                Width = surfaceTexture.Description.Width,
                MipLevels = 1,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging // GPU -> CPU
            };
            Texture2D texture2DFrame = new Texture2D(device, description);
            device.ImmediateContext.CopyResource(surfaceTexture, texture2DFrame);

            return texture2DFrame;

            //using (var frame = sender.TryGetNextFrame())
            //using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface))
            //{
            //    // Copy to our staging texture
            //    d3dContext.CopyResource(bitmap, stagingTexture);

            //    // Map our texture and get the bits
            //    var mapped = d3dContext.MapSubresource(stagingTexture, 0, SharpDX.Direct3D11.MapMode.Read, SharpDX.Direct3D11.MapFlags.None);
            //    var source = mapped.DataPointer;
            //    var sourceStride = mapped.RowPitch;

            //    // Allocate some memory to hold our copy
            //    var bytes = new byte[size.Width * size.Height * 4]; // 4 bytes per pixel
            //    unsafe
            //    {
            //        fixed (byte* bytesPointer = bytes)
            //        {
            //            var dest = (IntPtr)bytesPointer;
            //            var destStride = size.Width * 4;

            //            for (int i = 0; i < size.Height; i++)
            //            {
            //                SharpDX.Utilities.CopyMemory(dest, source, destStride);

            //                source = IntPtr.Add(source, sourceStride);
            //                dest = IntPtr.Add(dest, destStride);
            //            }
            //        }
            //    }

            //    // Don't forget to unmap when you're done!
            //    d3dContext.UnmapSubresource(stagingTexture, 0);

            //    // Encode it
            //    // NOTE: Waiting here will stall the capture
            //    EncodeBytesAsync($"image{imageNum}.png", size.Width, size.Height, bytes).Wait();
            //}
        }

        public static Texture2D GetLatestFrameAsTexture2D()
        {
            Direct3D11CaptureFrame frame = GetLatestFrame();

            if (frame is null)
                return null;

            return GetFrameAsTexture2D(frame);
        }

        private static void CopyMemory(
            bool parallel,
            int from,
            int to,
            IntPtr sourcePointer,
            IntPtr destinationPointer,
            int sourceStride,
            int destinationStride)
        {
            if (!parallel)
            {
                for (int i = from; i < to; i++)
                {
                    IntPtr sourceIteratedPointer = IntPtr.Add(sourcePointer, sourceStride * i);
                    IntPtr destinationIteratedPointer = IntPtr.Add(destinationPointer, destinationStride * i);

                    // Memcpy is apparently faster than Buffer.MemoryCopy. 
                    Utilities.CopyMemory(destinationIteratedPointer, sourceIteratedPointer, destinationStride);
                }
                return;
            }

            Parallel.For(from, to, i =>
            {
                IntPtr sourceIteratedPointer = IntPtr.Add(sourcePointer, sourceStride * i);
                IntPtr destinationIteratedPointer = IntPtr.Add(destinationPointer, destinationStride * i);

                // Memcpy is apparently faster than Buffer.MemoryCopy. 
                Utilities.CopyMemory(destinationIteratedPointer, sourceIteratedPointer, destinationStride);
            });
        }

        public static (byte[] frameBytes, int width, int height, int stride) GetFrameAsByteBgra(Texture2D frame)
        {
            Device device = CaptureHandler.GraphicCaptureDevice();

            DataBox mappedMemory = device.ImmediateContext.MapSubresource(frame, 0, MapMode.Read, MapFlags.None);

            if (frame.IsDisposed)
                return (null, 0, 0, 0);

            int width = frame.Description.Width;
            int height = frame.Description.Height;

            IntPtr sourcePointer = mappedMemory.DataPointer;
            int sourceStride = mappedMemory.RowPitch;
            int destinationStride = width * 4;

            byte[] frameBytes = new byte[width * height * 4]; // 4 bytes / pixel (High Mem. Allocation)

            unsafe
            {
                fixed (byte* frameBytesPointer = frameBytes)
                {
                    IntPtr destinationPointer = (IntPtr)frameBytesPointer;

                    /*
                    for (int i = 0; i < height; i++) {
                        Utilities.CopyMemory(destinationPointer, sourcePointer, destinationStride);

                        sourcePointer = IntPtr.Add(sourcePointer, sourceStride);
                        destinationPointer = IntPtr.Add(destinationPointer, destinationStride);
                    }
                    */

                    CopyMemory(
                        true, // Should run in parallel or not.
                        0,
                        height,
                        sourcePointer,
                        destinationPointer,
                        sourceStride,
                        destinationStride
                        );
                }
            }

            device.ImmediateContext.UnmapSubresource(frame, 0);
            frame.Dispose();

            return (frameBytes, width, height, destinationStride);
        }

        public static (byte[] frameBytes, int width, int height, int stride) GetLatestFrameAsByteBgra()
        {
            Texture2D frame = GetLatestFrameAsTexture2D();
            if (frame is null)
                return (null, 0, 0, 0);

            return GetFrameAsByteBgra(frame);
        }
    }
}
