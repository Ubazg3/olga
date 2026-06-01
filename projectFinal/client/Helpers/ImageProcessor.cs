using System;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace CheckersClient.Helpers
{
    // Profile-picture helpers. The user picks any image off disk; we
    // load it, square-crop, downscale to 256×256, and re-encode as a
    // small JPEG so what we send to the server stays under ~50 KB
    // regardless of what the original looks like.
    public static class ImageProcessor
    {
        public const int TargetSize = 256;
        public const int JpegQuality = 85;

        // Shows the standard Windows file-picker. Returns the absolute
        // path the user selected, or null if they cancelled.
        public static string PickImageFile()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title  = "Choose a profile picture",
                Filter = "Image files|*.jpg;*.jpeg;*.png;*.bmp;*.gif" +
                         "|All files|*.*",
                Multiselect = false
            };
            return dlg.ShowDialog() == true ? dlg.FileName : null;
        }

        // Loads a file, square-centre-crops it, scales to TargetSize ×
        // TargetSize, and returns JPEG bytes ready to ship.
        public static byte[] LoadAndProcess(string filePath)
        {
            // Read once into memory so the file isn't held open while
            // WPF's BitmapImage decodes it.
            byte[] raw = File.ReadAllBytes(filePath);

            BitmapImage src = new BitmapImage();
            using (var ms = new MemoryStream(raw))
            {
                src.BeginInit();
                src.CacheOption  = BitmapCacheOption.OnLoad;
                src.StreamSource = ms;
                src.EndInit();
                src.Freeze();
            }

            BitmapSource processed = SquareCropAndScale(src, TargetSize);
            return EncodeJpeg(processed, JpegQuality);
        }

        // ----- internals -----

        // Take the largest centred square of the source then scale it
        // to `size`×`size`. Cleaner than letterboxing for avatars.
        private static BitmapSource SquareCropAndScale(BitmapSource src, int size)
        {
            int w = src.PixelWidth;
            int h = src.PixelHeight;
            int side = Math.Min(w, h);
            int x = (w - side) / 2;
            int y = (h - side) / 2;

            CroppedBitmap cropped = new CroppedBitmap(src,
                new System.Windows.Int32Rect(x, y, side, side));

            double scale = (double)size / side;
            ScaleTransform xform = new ScaleTransform(scale, scale);
            TransformedBitmap scaled = new TransformedBitmap(cropped, xform);
            scaled.Freeze();
            return scaled;
        }

        private static byte[] EncodeJpeg(BitmapSource src, int quality)
        {
            JpegBitmapEncoder enc = new JpegBitmapEncoder { QualityLevel = quality };
            enc.Frames.Add(BitmapFrame.Create(src));
            using (var ms = new MemoryStream())
            {
                enc.Save(ms);
                return ms.ToArray();
            }
        }

        // Reverse: turn server-supplied bytes back into an ImageSource
        // a WPF Image control can render directly.
        public static BitmapImage BytesToImage(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return null;
            BitmapImage img = new BitmapImage();
            using (var ms = new MemoryStream(bytes))
            {
                img.BeginInit();
                img.CacheOption  = BitmapCacheOption.OnLoad;
                img.StreamSource = ms;
                img.EndInit();
                img.Freeze();
            }
            return img;
        }
    }
}
