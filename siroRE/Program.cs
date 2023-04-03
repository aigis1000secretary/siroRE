using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace siroRE
{
    internal class Program
    {
        static void Main(string[] args)
        {

            if (args.Length == 1)
            {
                // split game canva
                Mat canva = GetGameCanva(args[0]);
                //Cv2.ImShow("canva", canva);
                Console.WriteLine(canva);
                Cv2.WaitKey(0);
                Cv2.ImWrite("ss4.png", canva);
                return;
            }

            if (args.Length == 2)
            {
                // split args[0], ROI form args[1]
                // get canva
                Mat canva = Cv2.ImRead(args[0]);

                // get target range
                Mat search = Cv2.ImRead(args[1]);
                var roi = GetWorkRange(search, false, 1);

                //  scale mask
                double scale = (canva.Width * 1.0) / search.Width;
                // get search range
                var searchRange = new OpenCvSharp.Rect(
                   (int)(scale * roi.X), (int)(scale * roi.Y),
                   (int)(scale * roi.Width), (int)(scale * roi.Height)
                );

                // get mask
                Mat mask = new Mat(canva.Size(), MatType.CV_8UC1, new Scalar(0));
                Cv2.Rectangle(mask, searchRange, new Scalar(255), -1);

                // color inv
                Mat lut = new Mat(256, 1, MatType.CV_8UC1);
                for (int i = 0; i < 256; i++) { lut.Set<byte>(i, 0, (byte)(255 - i)); }
                Cv2.LUT(canva, lut, canva);

                // get target image
                Mat result = new Mat();
                Cv2.BitwiseAnd(canva, canva, result, mask);
                Cv2.LUT(result, lut, result);

                //Cv2.ImShow("mask", mask);
                //Cv2.ImShow("result", result);
                //Cv2.WaitKey(0);

                //Cv2.ImWrite("result.png", result);
                Cv2.ImWrite(args[1], result);
                return;
            }

            //if (args.Length == 2)
            //{
            //    // SSIM find args[1] in args[0]
            //    // get canva
            //    Mat canva = Cv2.ImRead(args[0]);

            //    // ss(5).png
            //    //Mat canva = GetGameCanva(args[0]);

            //    var result = SearchImage(canva, args[1]);

            //    //Console.WriteLine(result);
            //    Console.ReadKey();
            //    return;
            //}

            // loop logic
            //Console.WriteLine("請按任意鍵繼續 . . .");
            //Console.ReadKey();
            //Thread.Sleep(1000);

            while (true)
            {
                Mat canva = GetGameCanva();
                if (canva.Width == 0) { Thread.Sleep(1000); continue; }

                Cv2.NamedWindow("canva", WindowFlags.Normal);
                Cv2.ResizeWindow("canva", new OpenCvSharp.Size(canva.Width * 0.3, canva.Height * 0.3));
                Cv2.ImShow("canva", canva);

                var day = new string[] {
                    "daychange.png", "stamp.png", "info.png", "home.png",
                    "workshop.png", "build.png", "resultC.png",
                    "idle1.png", "idle2.png", "idle3.png", "idle4.png",
                    "done1.png", "done2.png", "done3.png", "done4.png"
                };
                var night = new string[] {
                    "workshop.png", "build.png", "resultC.png",
                    "idle1.png", "idle2.png", "idle3.png", "idle4.png",
                    "done1.png", "done2.png", "done3.png", "done4.png"
                };

                foreach (string filename in (2 <= DateTime.Now.Hour && DateTime.Now.Hour < 6) ? day : night)
                {
                    string filepath = @"Resource\" + filename;
                    // // timer
                    // Stopwatch stopWatch = new Stopwatch();
                    // stopWatch.Start();
                    if (Click(canva, filepath)) { break; };
                    // stopWatch.Stop();
                    // Console.WriteLine(stopWatch.ElapsedMilliseconds + "ms");
                }


                //Thread.Sleep(1000);
                int key = Cv2.WaitKey(1000);

                canva.Dispose();

                if (key == 27) { break; } // esc
            }

            //Console.ReadKey();
        }

        static bool Click(Mat canva, string filepath)
        {
            var result = SearchImage(canva, filepath);

            if (result.Width == 0) { return false; }

            Random rand = new Random(Guid.NewGuid().GetHashCode());

            int x = WorkRange.X + result.X + rand.Next(0, result.Width);
            int y = WorkRange.Y + result.Y + rand.Next(0, result.Height);

            mouse.Click(x, y);
            return true;
        }

        static Bitmap Screenshot()
        {
            var Bounds = Screen.PrimaryScreen.Bounds;
            var bmpScreenshot = new Bitmap(Bounds.Width, Bounds.Height, PixelFormat.Format32bppArgb);

            while (true)
            {
                try
                {
                    var gfxScreenshot = Graphics.FromImage(bmpScreenshot);
                    gfxScreenshot.CopyFromScreen(Bounds.X, Bounds.Y, 0, 0,
                        Screen.PrimaryScreen.Bounds.Size,
                        CopyPixelOperation.SourceCopy);
                }
                catch (Exception e)
                {
                    Thread.Sleep(1000);
                    continue;
                }
                break;
            }

            return new Bitmap(bmpScreenshot);
        }

        static OpenCvSharp.Rect WorkRange;
        static Mat GetGameCanva(string filepath = null)
        {
            // get screenshot from file
            using (Mat screen = filepath == null ?
                BitmapConverter.ToMat(Screenshot()) :
                Cv2.ImRead(filepath, ImreadModes.Unchanged))
            {
                // get work range
                var newRange = GetWorkRange(screen);

                double ratio = (newRange.Width * 1.0) / newRange.Height;
                //Console.WriteLine("newRange: " + newRange);
                Console.WriteLine("Canva Ratio: " + ratio);

                if (1.72 < ratio && ratio < 1.82)
                //if (newRange.Width > 2 && newRange.Height > 2)
                {
                    WorkRange = newRange;
                }

                // get game canva
                return new Mat(screen, WorkRange);
            }
        }

        static Dictionary<string, Mat> SearchMemery = new Dictionary<string, Mat>();
        static Dictionary<string, OpenCvSharp.Rect> WorkRangeMemery = new Dictionary<string, OpenCvSharp.Rect>();

        static OpenCvSharp.Rect SearchImage(Mat canva, string filepath, double valve = 0.95)
        {

            // get target from file
            Mat search = SearchMemery.ContainsKey(filepath) ?
                SearchMemery[filepath] :
                Cv2.ImRead(filepath, ImreadModes.Unchanged);
            // keep data
            if (!SearchMemery.ContainsKey(filepath))
            { SearchMemery[filepath] = search; }

            // check search range
            if (search.Height == 0 || canva.Height == 0 ||
                Math.Abs((search.Width / search.Height) - (canva.Width / canva.Height)) > 0.05)
            {
                // unknown canva ratio
                return new OpenCvSharp.Rect(0, 0, 0, 0);
            }

            // get search target roi
            // var roi = GetWorkRange(search, false, 1);
            var roi = WorkRangeMemery.ContainsKey(filepath) ?
                WorkRangeMemery[filepath] :
                GetWorkRange(search, false, 1);
            // keep data
            if (!WorkRangeMemery.ContainsKey(filepath))
            { WorkRangeMemery[filepath] = roi; }

            Console.Write(roi + "  ");

            // get scale
            double scale = (canva.Width * 1.0) / search.Width;
            // get search range
            var searchRange = new OpenCvSharp.Rect(
               (int)(scale * roi.X), (int)(scale * roi.Y),
               (int)(scale * roi.Width), (int)(scale * roi.Height)
            );

            if (roi.Width == 0 || searchRange.Width == 0 ||
                roi.Height == 0 || searchRange.Height == 0)
            {
                return new OpenCvSharp.Rect(0, 0, 0, 0);
            }

            Mat target = new Mat(search, roi);
            Mat range = new Mat(canva, searchRange);

            // scale for SSIM
            Cv2.Resize(target, target, new OpenCvSharp.Size(8, 8));
            Cv2.Resize(range, range, new OpenCvSharp.Size(8, 8));

            //Console.WriteLine(search);
            //Console.WriteLine(canva);
            //Cv2.ImShow("target", target);
            //Cv2.ImShow("range", range);
            //Cv2.ImWrite("r1.png", target);
            //Cv2.ImWrite("r2.png", range);

            //Console.WriteLine(canva);
            //Console.WriteLine(search);
            //Cv2.ImShow("canva", canva);
            //Cv2.ImShow("search", search);

            //Console.WriteLine(target);
            //Console.WriteLine(range);
            //Cv2.WaitKey(0);

            SSIMResult ssim = GetMSSIM(target, range);
            Console.WriteLine("SSIM (" + filepath + "): " + ssim.Score);

            target.Dispose();
            range.Dispose();

            //Cv2.WaitKey(0);
            if (ssim.Score < valve)
            {
                return new OpenCvSharp.Rect(0, 0, 0, 0);
            }

            return searchRange;
        }

        public static SSIMResult GetMSSIM(Mat i1, Mat i2)
        {
            Scalar mssim;
            try
            {
                if (i1.Type() == MatType.CV_8UC4)
                { Cv2.CvtColor(i1, i1, ColorConversionCodes.BGRA2BGR); }

                if (i2.Type() == MatType.CV_8UC4)
                { Cv2.CvtColor(i2, i2, ColorConversionCodes.BGRA2BGR); }

                const double C1 = 6.5025, C2 = 58.5225;
                /***************************** INITS **********************************/
                MatType d = MatType.CV_32F;

                Mat I1 = new Mat(), I2 = new Mat();
                i1.ConvertTo(I1, d); // cannot calculate on one byte large values
                i2.ConvertTo(I2, d);

                Mat I2_2 = I2.Mul(I2); // I2^2
                Mat I1_2 = I1.Mul(I1); // I1^2
                Mat I1_I2 = I1.Mul(I2); // I1*I2

                /*********************** PRELIMINARY COMPUTING ******************************/

                Mat mu1 = new Mat(), mu2 = new Mat(); //
                Cv2.GaussianBlur(I1, mu1, new OpenCvSharp.Size(11, 11), 1.5);
                Cv2.GaussianBlur(I2, mu2, new OpenCvSharp.Size(11, 11), 1.5);

                I1.Dispose();
                I2.Dispose();

                Mat mu1_2 = mu1.Mul(mu1);
                Mat mu2_2 = mu2.Mul(mu2);
                Mat mu1_mu2 = mu1.Mul(mu2);

                mu1.Dispose();
                mu2.Dispose();

                Mat sigma1_2 = new Mat(), sigma2_2 = new Mat(), sigma12 = new Mat();
                Cv2.GaussianBlur(I1_2, sigma1_2, new OpenCvSharp.Size(11, 11), 1.5);
                sigma1_2 -= mu1_2;
                Cv2.GaussianBlur(I2_2, sigma2_2, new OpenCvSharp.Size(11, 11), 1.5);
                sigma2_2 -= mu2_2;
                Cv2.GaussianBlur(I1_I2, sigma12, new OpenCvSharp.Size(11, 11), 1.5);
                sigma12 -= mu1_mu2;

                I2_2.Dispose();
                I1_2.Dispose();
                I1_I2.Dispose();

                // FORMULA
                Mat t1, t2, t3;

                t1 = 2 * mu1_mu2 + C1;
                t2 = 2 * sigma12 + C2;
                t3 = t1.Mul(t2); // t3 = ((2 * mu1_mu2 + C1).*(2 * sigma12 + C2))

                t1 = mu1_2 + mu2_2 + C1;
                t2 = sigma1_2 + sigma2_2 + C2;
                t1 = t1.Mul(t2); // t1 = ((mu1_2 + mu2_2 + C1).*(sigma1_2 + sigma2_2 + C2))

                mu1_2.Dispose();
                mu2_2.Dispose();
                mu1_mu2.Dispose();
                sigma1_2.Dispose();
                sigma2_2.Dispose();
                sigma12.Dispose();

                Mat ssim_map = new Mat();
                Cv2.Divide(t3, t1, ssim_map); // ssim_map = t3./t1;

                mssim = Cv2.Mean(ssim_map); // mssim = average of ssim map

                t1.Dispose();
                t2.Dispose();
                t3.Dispose();
                ssim_map.Dispose();
            }
            catch (Exception e)
            {
                //Console.WriteLine(e);
                mssim = new Scalar(-3, 0, 0);
            }

            SSIMResult result = new SSIMResult();
            result.mssim = mssim;
            //result.diff = ssim_map;

            return result;
        }

        static OpenCvSharp.Rect GetWorkRange(Mat screen, bool ByAlpha = false, int valve = 20)
        {
            bool pixel(int y, int x)
            {
                if (ByAlpha && screen.Type() == MatType.CV_8UC4)
                {
                    Vec4b bgra = screen.At<Vec4b>(y, x);
                    // byte b = bgra.Item0; byte g = bgra.Item1; byte r = bgra.Item2;
                    byte a = bgra.Item3;

                    if (a == 255) { return true; }
                }
                else
                {
                    Vec3b bgr = screen.At<Vec3b>(y, x);
                    byte b = bgr.Item0; byte g = bgr.Item1; byte r = bgr.Item2;

                    if ((r + g + b) / 3 < 240) { return true; }
                    //if (r * 0.299 + g * 0.587 + b * 0.114 < 200) {return true; }
                }
                return false;
            };


            int left = 0;
            int right = screen.Cols;
            int top = 0;
            int bottom = screen.Rows;
            bool found;

            //int cx = (int)(right / 2);
            //int cy = (int)(bottom / 2);

            found = false;
            for (int y = top; y < bottom; ++y)
            {
                int count = 0;
                for (int x = left; x < right; ++x)
                {
                    // check pixel white/transparent
                    if (pixel(y, x)) { ++count; }
                    if (count > valve) { break; }
                }

                if (count <= valve) { top = y + 1; found = true; }
                else if (found) { break; }
            }

            found = false;
            for (int y = bottom - 1; y >= top; --y)
            {
                int count = 0;
                for (int x = right - 1; x >= left; --x)
                {
                    // check pixel white/transparent
                    if (pixel(y, x)) { ++count; }
                    if (count > valve) { break; }
                }

                if (count <= valve) { bottom = y; found = true; }
                else if (found) { break; }
            }

            found = false;
            for (int x = left; x < right; ++x)
            {
                int count = 0;
                for (int y = top; y < bottom; ++y)
                {
                    // check pixel white/transparent
                    if (pixel(y, x)) { ++count; }
                    if (count > valve) { break; }
                }

                if (count <= valve) { left = x + 1; found = true; }
                else if (found) { break; }
            }

            found = false;
            for (int x = right - 1; x >= left; --x)
            {
                int count = 0;
                for (int y = bottom - 1; y >= top; --y)
                {
                    // check pixel white/transparent
                    if (pixel(y, x)) { ++count; }
                    if (count > valve) { break; }
                }

                if (count <= valve) { right = x; found = true; }
                else if (found) { break; }
            }

            //Console.WriteLine("ROI: " + left + ", " + top + ", " + right + ", " + bottom);
            //Console.WriteLine("ROI: " + left + ", " + top + ", " + (right - left) + ", " + (bottom - top));
            return new OpenCvSharp.Rect(left, top, right - left, bottom - top);
            //return null;
        }
    }

    public class SSIMResult
    {
        public double Score
        {
            get
            {
                return (mssim.Val0 + mssim.Val1 + mssim.Val2) / 3;
            }
        }
        public Scalar mssim;
        //public Mat diff;
    }
}
