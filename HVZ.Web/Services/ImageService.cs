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

            Directory.CreateDirectory(Path.Combine(uploadPath, "users"));
            Directory.CreateDirectory(Path.Combine(uploadPath, "orgs"));
        }

        /// <summary>
        /// Retreive the resource path for a specific User thumbnail. Does not check that the file exists.
        /// </summary>
        /// <param name="id">The User ID associated with the thumbnail</param>
        /// <param name="imageSize">Desired image size</param>
        /// <returns>The path to the thumbnail web resource</returns>
        public string GetUserThumbnailResourceLink(string id, ImageSize imageSize) => $"images/users/{id}_thumbnail_{(int)(imageSize)}.jpeg";

        /// <summary>
        /// Retreive the resource path for a specific Org thumbnail. Does not check that the file exists.
        /// </summary>
        /// <param name="id">The Org ID associated with the thumbnail</param>
        /// <param name="imageSize">Desired image size</param>
        /// <returns>The path to the thumbnail web resource</returns>
        public string GetOrgThumbnailResourceLink(string id, ImageSize imageSize) => $"images/orgs/{id}_thumbnail_{(int)(imageSize)}.jpeg";

        /// <summary>
        /// Check if there is a saved image for the user Id
        /// </summary>
        /// <param name="id">User ID to check against</param>
        /// <returns>Whether there is an uploaded file for the ID</returns>
        public virtual bool HasUploadedUserImage(string id) => File.Exists(Path.Combine(uploadPath, "users", $"{id}_thumbnail_128.jpeg"));

        /// <summary>
        /// Check if there is a saved image for the org ID
        /// </summary>
        /// <param name="id">Org ID to check against</param>
        /// <returns>Whether there is an uploaded file for the ID</returns>
        public virtual bool HasUploadedOrgImage(string id) => File.Exists(Path.Combine(uploadPath, "orgs", $"{id}_thumbnail_128.jpeg"));

        /// <summary>
        /// Write an uploaded file to the disk under the "user" sub folder along with small 64x64 px, medium 128x128 px, and large 256x256 px thumbnail images
        /// </summary>
        /// <param name="file"></param>
        /// <param name="id">The User ID associated with the file</param>
        public virtual async Task SaveUserImage(IBrowserFile file, string id)
            => await SaveImage(file, id, "users");

        /// <summary>
        /// Write an uploaded file to the disk under the "org" sub folder along with small 64x64 px, medium 128x128 px, and large 256x256 px thumbnail images
        /// </summary>
        /// <param name="file"></param>
        /// <param name="id">The Org ID associated with the file</param>
        public virtual async Task SaveOrgImage(IBrowserFile file, string id)
            => await SaveImage(file, id, "orgs");

        /// <summary>
        /// Write an uploaded file to the disk and creates small 64x64 px, medium 128x128 px, and large 256x256 px thumbnail files
        /// </summary>
        /// <param name="file"></param>
        /// <param name="fileName">File name, excluding extension</param>
        /// <param name="folder">Sub folder in the upload directory to save the image</param>
        public virtual async Task SaveImage(IBrowserFile file, string fileName, string folder)
        {
            // Check that the file is png, jpg, or jpeg
            var fileContentType = file.ContentType.Split("/");
            if (fileContentType[0] != "image")
            {
                throw new ArgumentException("File must be an image");
            }

            var path = Path.Combine(uploadPath, folder);
            await using FileStream fs = new FileStream(Path.Combine(path, $"{fileName}.{fileContentType[1]}"), FileMode.Create);
            await file.OpenReadStream(4096 * 4096 * 32).CopyToAsync(fs);
            fs.Close();
            await SaveThumbnails(path, fileName);
        }

        private async Task SaveThumbnails(string path, string imageName)
        {
            await using FileStream fs = new FileStream(path, FileMode.Open);
            using (var stream = new SKManagedStream(fs))
            {
                SKBitmap src = SKBitmap.Decode(stream);

                await SaveBitmap(
                    CropSquare(src, (int)(ImageSize.SMALL)),
                    Path.Combine(path, $"{imageName}_thumbnail_{(int)ImageSize.SMALL}.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                await SaveBitmap(
                    CropSquare(src, (int)(ImageSize.MEDIUM)),
                    Path.Combine(path, $"{imageName}_thumbnail_{(int)ImageSize.MEDIUM}.jpeg"),
                    SKEncodedImageFormat.Jpeg,
                    100
                );

                await SaveBitmap(
                    CropSquare(src, (int)(ImageSize.LARGE)),
                    Path.Combine(path, $"{imageName}_thumbnail_{(int)ImageSize.LARGE}.jpeg"),
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