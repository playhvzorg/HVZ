using HVZ.Web.Settings;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.IO;

namespace HVZ.Web.Services
{
    public class ImageService
    {
        private string uploadPath;

        public ImageService(IOptions<ImageServiceOptions> options)
        {
            this.uploadPath = options.Value.UploadPath;
            // this.uploadPath = "wwwroot\\images";
        }

        public string GetThumbnailSmall(string id) => $"images/{id}_thumbnail_small.jpeg";

        public string GetThumbnailMedium(string id) => $"images/{id}_thumbnail_medium.jpeg";

        public string GetThumbnailLarge(string id) => $"images/{id}_thumbnail_large.jpeg";

        public string GetThumbnail(string id, string size) => $"images/{id}_thumbnail_{size}.jpeg";

        public async Task SaveImage(IBrowserFile file, string imageName)
        {
            // Check that the file is png, jpg, or jpeg
            var fileContentType = file.ContentType.Split("/");
            if (fileContentType[0] != "image")
            {
                // TODO: Notify that the file is not a correct type
                return;
            }

            var path = Path.Combine(uploadPath, $"{imageName}.{fileContentType[1]}");
            await using FileStream fs = new FileStream(path, FileMode.Create);
            await file.OpenReadStream(2048 * 2048 * 32).CopyToAsync(fs);
            fs.Close();
            await SaveThumbnails(path, imageName);
        }

        private async void SaveThumbnails(string path, string imageName)
        {
            await using FileStream fs = new FileStream(path, FileMode.Open);
            using (var stream = new SKManagedStream(fs))
            {
                SKBitmap src = SKBitmap.Decode(stream);

                await SaveBitmap(
                    CropSquare(src, 64),
                    Path.Combine(uploadPath, $"{imageName}_thumbnail_small.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                await SaveBitmap(
                    CropSquare(src, 128),
                    Path.Combine(uploadPath, $"{imageName}_thumbnail_medium.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                await SaveBitmap(
                    CropSquare(src, 256),
                    Path.Combine(uploadPath, $"{imageName}_thumbnail_large.jpeg"),
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
    }
}