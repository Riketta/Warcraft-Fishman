using OpenCvSharp;
using System;
using System.ComponentModel;
using System.Diagnostics;
using VersaScreenCapture;

namespace Fishman
{
    internal class FrameProcessor
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public bool Debug { get; set; }

        private readonly Mat _template = null;
        private readonly ImreadModes _imageReadMode = ImreadModes.Color;

        public FrameProcessor(string pathToTemplate, ImreadModes imreadMode = ImreadModes.Color)
        {
            _imageReadMode = imreadMode;
            _template = new Mat(pathToTemplate, _imageReadMode);
            if (_template.Height == 0 || _template.Width == 0)
                throw new ArgumentException("Empty template loaded!");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="region"></param>
        /// <returns>The maximum match value found in the range [0; 1]..</returns>
        /// <exception cref="ArgumentNullException"></exception>
        public double BiteDetection(System.Drawing.Rectangle region)
        {
            if (_template is null)
                throw new ArgumentNullException();

            (Mat frame, _, _) = GetLatestFrameAsMat();
            if (frame is null)
                return 0;

            Mat fixedFrame = frame.CvtColor(ColorConversionCodes.BGRA2BGR);
            if (Debug)
            {
                string filename = $"frame_{DateTime.Now.Ticks}_source.png";
                fixedFrame.SaveImage(filename);
                Console.WriteLine($"Pre-processed image of whole frame saved as: \"{filename}\"");
            }

            Mat targetRegionFrame = fixedFrame.SubMat(new Rect(region.Left, region.Top, region.Width, region.Height));
            if (Debug)
            {
                string filename = $"frame_{DateTime.Now.Ticks}_target.png";
                targetRegionFrame.SaveImage(filename);
                Console.WriteLine($"Pre-processed image of target region saved as: \"{filename}\"");
            }

            //Cv2.ImShow("Frame", subFrame);
            //Cv2.WaitKey();
            double maxValue = MatchFrame(targetRegionFrame, _template);

            frame.Dispose();
            fixedFrame.Dispose();
            targetRegionFrame.Dispose();

            return maxValue;
        }

        (Mat, int width, int height) GetLatestFrameAsMat()
        {
            (byte[] frameBytes, int width, int height, int stride) = FramePool.GetLatestFrameAsByteBgra();
            if (frameBytes == null) return (null, 0, 0);

            /*
            Mat frameMat = new Mat(width, height, MatType.CV_8UC4); // 8UC4: 8 unsigned bits * 4 colors (BGRA)
            Mat.Indexer<Vec4b> indexer = frameMat.GetGenericIndexer<Vec4b>();

            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    int bufferPos = y * width + x * 4;
                    // BGRA format
                    byte blue = frameBytes[bufferPos];
                    byte green = frameBytes[bufferPos + 1];
                    byte red = frameBytes[bufferPos + 2];
                    byte alpha = frameBytes[bufferPos + 3];
                    Vec4b matByteValue = new Vec4b(blue, green, red, alpha);
                    indexer[y, x] = matByteValue;
                }
            }
            */

            // 8UC4: 8 unsigned bits * 4 colors (BGRA), Padding: width * (4 bytes / pixel)
            Mat frameMat = new Mat(height, width, MatType.CV_8UC4, frameBytes, stride);

            return (frameMat, width, height);
        }

        public double MatchFrame(Mat frame, Mat template)
        {
            //Mat matchResult = new Mat(frame.Rows - template.Rows + 1, frame.Cols - template.Cols + 1, MatType.CV_32FC1);
            Mat matchResult = new Mat(template.Rows, template.Cols, MatType.CV_32FC1);
            Cv2.MatchTemplate(frame, template, matchResult, TemplateMatchModes.CCoeffNormed);
            Cv2.MinMaxLoc(matchResult, out _, out double maxValue, out _, out Point maxLocation);

            //Cv2.ImShow("Heat Map", matchResult);
            //Cv2.WaitKey();

            if (Debug)
            {
                Rect border = new Rect(new Point(maxLocation.X, maxLocation.Y), new Size(template.Width, template.Height));
                Cv2.Rectangle(frame, border, Scalar.LimeGreen, 1);

                //Cv2.ImShow("Match", frame);
                //Cv2.WaitKey();
            }

            return maxValue;
        }
    }
}
