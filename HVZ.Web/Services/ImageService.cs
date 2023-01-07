using HVZ.Web.Settings;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace HVZ.Web.Services
{
    public class ImageService
    {
        private string uploadPath;

        public ImageService()
        {
            this.uploadPath = "";
        }

        public ImageService(IOptions<ImageServiceOptions> options)
        {
            if (options.Value.UploadPath is null) throw new ArgumentNullException("UploadPath cannot be null in ImageService");

            this.uploadPath = options.Value.UploadPath;
        }

        /// <summary>
        /// Retreive the resource path for a specific thumbnail. Does not check that the file exists.
        /// </summary>
        /// <param name="id">The ID associated with the thumbnail</param>
        /// <param name="imageSize">Desired image size</param>
        /// <returns>The path to the thumbnail</returns>
        public string GetThumbnailResourceLink(string id, ImageSize imageSize) => $"images/{id}_thumbnail_{(int)(imageSize)}.jpeg";

        /// <summary>
        /// Check if there is a saved image associated with the ID
        /// </summary>
        /// <param name="id">ID to check against</param>
        /// <returns>Whether there is an uploaded file for the ID</returns>
        public virtual bool HasUploadedImage(string id) => File.Exists(Path.Combine(uploadPath, $"{id}_thumbnail_128.jpeg"));

        /// <summary>
        /// Write an uploaded file to the disk and creates small, medium, and large thumbnail files
        /// </summary>
        /// <param name="file"></param>
        /// <param name="id">The ID associated with the image (user or org)</param>
        public virtual async Task SaveImage(IBrowserFile file, string id)
        {
            // Check that the file is png, jpg, or jpeg
            var fileContentType = file.ContentType.Split("/");
            if (fileContentType[0] != "image")
            {
                throw new ArgumentException("File must be an image");
            }

            var path = Path.Combine(uploadPath, $"{id}.{fileContentType[1]}");
            await using FileStream fs = new FileStream(path, FileMode.Create);
            await file.OpenReadStream(4096 * 4096 * 32).CopyToAsync(fs);
            fs.Close();
            await SaveThumbnails(path, id);
        }

        private async Task SaveThumbnails(string path, string imageName)
        {
            await using FileStream fs = new FileStream(path, FileMode.Open);
            using (var stream = new SKManagedStream(fs))
            {
                SKBitmap src = SKBitmap.Decode(stream);

                await SaveBitmap(
                    CropSquare(src, (int)(ImageSize.SMALL)),
                    Path.Combine(uploadPath, $"{imageName}_thumbnail_{(int)(ImageSize.SMALL)}.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                await SaveBitmap(
                    CropSquare(src, (int)(ImageSize.MEDIUM)),
                    Path.Combine(uploadPath, $"{imageName}_thumbnail_{(int)(ImageSize.MEDIUM)}.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                await SaveBitmap(
                    CropSquare(src, (int)(ImageSize.LARGE)),
                    Path.Combine(uploadPath, $"{imageName}_thumbnail_{(int)(ImageSize.LARGE)}.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                src.Dispose();
            }
            fs.Close();
        }

        private async Task SaveBitmap(SKBitmap bitmap, string destination, SKEncodedImageFormat format, int quality)
        {
            await using FileStream fs = new FileStream(destination, FileMode.Create);
            SKData d = SKImage.FromBitmap(bitmap).Encode(format, quality);
            d.SaveTo(fs);
            fs.Close();
        }

        private SKBitmap CropSquare(SKBitmap src, int size)
        {
            // Crop to square
            int difference = src.Width - src.Height;
            var cropRect = new SKRectI
            {
                Left = 0,
                Top = 0,
                Right = src.Width,
                Bottom = src.Height
            }; // Capture entire image

            if (difference < 0)
            {
                // Vertical image - chop top and bottom
                cropRect.Top = -difference / 2;
                cropRect.Bottom = src.Height + (difference / 2);
            }
            else if (difference > 0)
            {
                // Horizontal image - cop left and right
                cropRect.Left = difference / 2;
                cropRect.Right = src.Width - (difference / 2);
            }
            // if difference is 0 image is already a square

            // Extract from the original image
            SKBitmap cropped = new SKBitmap(cropRect.Width, cropRect.Height);
            src.ExtractSubset(cropped, cropRect);

            // Scale new square to size
            SKBitmap scaled = new SKBitmap(size, size);
            cropped.ScalePixels(scaled, SKFilterQuality.High);

            return scaled;
        }

        public enum ImageSize
        {
            SMALL = 64,
            MEDIUM = 128,
            LARGE = 256
        }
    }
}