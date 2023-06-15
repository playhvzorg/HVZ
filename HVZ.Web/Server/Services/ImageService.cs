using HVZ.Web.Server.Services.Settings;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace HVZ.Web.Server.Services
{
    public class ImageService
    {
        private readonly string _imageDirectory;
        private readonly string[] _subdirectories = new[] { "user", "org" };

        public ImageService(IOptions<ImageConfig> options)
        {
            if (options.Value.ImageDirectory is null)
            {
                _imageDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PlayHVZ", "images");
            }
            else
            {
                // TODO: Check that this is an absolute directory and not a relative direcory
                _imageDirectory = options.Value.ImageDirectory;
            }

            // Create the image directory if it does not already exist
            //Directory.CreateDirectory(_imageDirectory);
        }

        private SKBitmap LoadBitmap(Stream stream, out SKEncodedOrigin origin)
        {
            using (var s = new SKManagedStream(stream))
            {
                using (var codec = SKCodec.Create(s))
                {
                    origin = codec.EncodedOrigin;
                    var info = codec.Info;
                    var bitmap = new SKBitmap(
                        info.Width,
                        info.Height,
                        SKImageInfo.PlatformColorType,
                        info.IsOpaque ? SKAlphaType.Opaque : SKAlphaType.Premul);

                    IntPtr length;
                    var result = codec.GetPixels(bitmap.Info, bitmap.GetPixels(out length));
                    if (result == SKCodecResult.Success || result == SKCodecResult.IncompleteInput)
                        return bitmap;

                    throw new ArgumentException("Unable to load bitmap from provided stream");
                }
            }
        }

        /// <summary>
        /// Save an image file to the disc as a 256x256 thumbnail image
        /// </summary>
        /// <exception cref="InvalidSubDirectoryException">The specified subdirectory is not a valid subdirectory</exception>
        public async Task SaveImage(Stream imageStream, string fileName, string subdir)
        {
            if (!_subdirectories.Contains(subdir))
            {
                throw new InvalidSubDirectoryException($"{subdir} is not a valid image directory");
            }

            string path = Path.Combine(_imageDirectory, subdir);
            // Create the subdirectory if it does not exist already
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            //using var managedImageStream = new SKManagedStream(imageStream);


            // Crop the image as a 256x256 square with High filter quality
            //using var image = SKBitmap.Decode(managedImageStream);
            //using var codec = SKCodec.Create(SKImage.FromBitmap(image).Encode(SKEncodedImageFormat.Jpeg, 100));

            SKEncodedOrigin origin;
            using var image = LoadBitmap(imageStream, out origin);

            using var cropped = await CropSquareAsync(image);
            using var rotated = await ExifHeaderRotate(cropped, origin);
            cropped.Dispose();
            await using FileStream fs = new FileStream(
                Path.Combine(path, $"{fileName}.jpeg"),
                FileMode.Create);

            // Save to location
            Console.WriteLine("Hello");
            SKData d = SKImage.FromBitmap(rotated).Encode(SKEncodedImageFormat.Jpeg, 100);
            d.SaveTo(fs);
            fs.Close();
        }

        private async Task<SKBitmap> CropSquareAsync(SKBitmap src)
        {
            int size = 256;

            int difference = src.Width - src.Height;
            var cropRect = new SKRectI
            {
                Left = 0,
                Top = 0,
                Right = src.Width,
                Bottom = src.Height,
            };

            if (difference < 0) // Image height > width
            {
                cropRect.Top = -difference / 2;
                cropRect.Bottom = src.Height + (difference / 2);
            }
            else if (difference > 0) // Image width > height
            {
                cropRect.Left = difference / 2;
                cropRect.Right = src.Width - (difference / 2);
            }
            // if difference is 0, image is already a square


            using SKBitmap cropped = new SKBitmap(cropRect.Width, cropRect.Height);
            await Task.Run(() => src.ExtractSubset(cropped, cropRect));

            SKBitmap scaled = new SKBitmap(size, size);
            await Task.Run(() => cropped.ScalePixels(scaled, SKFilterQuality.High));

            return scaled;
        }

        /// <summary>
        /// Rotate an image based on its EXIF header
        /// </summary>
        /// <remarks>
        /// Assumes the image is already cropped to a square
        /// </remarks>
        /// <param name="src"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private async Task<SKBitmap> ExifHeaderRotate(SKBitmap src, SKEncodedOrigin origin)
        {
            if (src.Width != src.Height)
            {
                throw new ArgumentException("src height must be equal to src width");
            }

            //if (origin == SKEncodedOrigin.TopLeft) return src;

            var bitmap = new SKBitmap(src.Width, src.Height);

            IntPtr srcPixelAddr = src.GetPixels();

            uint[,] pixelBuffer = new uint[src.Width, src.Height];

            Console.WriteLine(SKEncodedOrigin.Default == SKEncodedOrigin.TopLeft);

            // This is fast but ugly
            unsafe
            {
                uint* srcPtr = (uint*)srcPixelAddr.ToPointer();

                switch (origin)
                {
                    case SKEncodedOrigin.Default:
                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[x, y] = *srcPtr++;
                        break;

                    case SKEncodedOrigin.LeftBottom:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[y, src.Width - 1 - x] = *srcPtr++;

                        break;
                    case SKEncodedOrigin.RightTop:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[src.Height - 1 - y, x] = *srcPtr++;

                        break;
                    case SKEncodedOrigin.RightBottom:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[src.Height - 1 - y, src.Width - 1 - x] = *srcPtr++;

                        break;
                    case SKEncodedOrigin.LeftTop:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[y, x] = *srcPtr++;
                        break;
                    case SKEncodedOrigin.BottomLeft:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[x, src.Height - 1 - y] = *srcPtr++;
                        break;
                    case SKEncodedOrigin.BottomRight:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[src.Width - 1 - x, src.Height - 1 - y] = *srcPtr++;
                        break;
                    case SKEncodedOrigin.TopRight:

                        for (int x = 0; x < src.Width; x++)
                            for (int y = 0; y < src.Height; y++)
                                pixelBuffer[src.Width - 1 - x, y] = *srcPtr++;
                        break;
                }

                fixed (uint* bitmapPtr = pixelBuffer)
                {
                    bitmap.SetPixels((IntPtr)bitmapPtr);
                }
            }

            src.Dispose();

            return bitmap;
        }

        unsafe uint* GetPixelFromPtr(uint* src, int x, int y, int width)
        {
            return src + width * x + y;
        }

        public string? GetImagePath(string fileName, string subdir)
        {
            string path = Path.Combine(_imageDirectory, subdir, $"{fileName}.jpeg");
            return File.Exists(path) ? path : null;
        }

    }

    public class InvalidSubDirectoryException : ApplicationException
    {
        public InvalidSubDirectoryException(string message) : base(message) { }
    }
}
