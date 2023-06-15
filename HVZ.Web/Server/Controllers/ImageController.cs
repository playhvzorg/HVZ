using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Web.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SkiaSharp;
using System.Diagnostics;

namespace HVZ.Web.Server.Controllers
{
    [Route("[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class ImageController : ControllerBase
    {
        private readonly ILogger<ImageController> _logger;
        private readonly ImageService _imageService;
        private readonly long _maxContentSize = 5242880; // 5MB
        private readonly List<string> categories = new() { "user", "org" };
        private readonly IOrgRepo _orgRepo;

        public ImageController(ILogger<ImageController> logger, ImageService imageService, IOrgRepo orgRepo)
        {
            _logger = logger;
            _imageService = imageService;
            _orgRepo = orgRepo;
        }

        [HttpGet("test")]
        public async Task<PhysicalFileResult> GetTestImage()
        {
            return new PhysicalFileResult("C:\\Users\\mcnin\\Downloads\\blazorshitpost.png", "image/png");
        }

        [HttpGet("streamtest")]
        public async Task<FileStreamResult> GetTestImageStream()
        {
            return new FileStreamResult(
                System.IO.File.OpenRead("C:\\Users\\mcnin\\Downloads\\blazorshitpost.png"), "image/png");
        }

        [HttpGet("resizetest")]
        public async Task<FileResult> GetResizedImage()
        {
            // start a stopwatch
            Stopwatch stopwatch = new Stopwatch();
            await using (FileStream fs = new FileStream("C:\\Users\\mcnin\\Downloads\\blazorshitpost.png", FileMode.Open))
            {
                stopwatch.Start();
                SKBitmap src = SKBitmap.Decode(fs);
                if (src is null)
                    throw new NullReferenceException();

                SKBitmap scaled = new SKBitmap(512, 512);
                src.ScalePixels(scaled, SKFilterQuality.High);

                Stream imageStream = SKImage.FromBitmap(scaled).Encode(SKEncodedImageFormat.Png, 100).AsStream();
                stopwatch.Stop();

                _logger.LogInformation($"Image scale took: {stopwatch.Elapsed.Milliseconds} ms");

                stopwatch.Reset();

                return new FileStreamResult(imageStream, "image/png");
            }
        }

        [HttpGet("{category}/{id}")]
        public ActionResult GetImage(string category, string id)
        {
            var imagePath = _imageService.GetImagePath(id, category);
            // Use identicon/default avatar here instead of relying on UI avatars?
            return imagePath is not null ? new PhysicalFileResult(imagePath, "image/jpeg") : NotFound();
        }

        [HttpPost("user/upload")]
        [Authorize]
        public async Task<ActionResult> SetUserImage([FromForm] IFormFile file)
        {
            string? userId = User.Claims.FirstOrDefault(c => c.Type == "DatabaseId")?.Value;
            if (userId is null)
                return Unauthorized();

            return await SaveImage("user", userId, file);
        }

        [HttpPost("org/{id}/upload")]
        [Authorize]
        public async Task<ActionResult> SetOrgImage(string id, [FromForm] IFormFile file)
        {
            Organization? org = await _orgRepo.FindOrgById(id);
            if (org is null)
                return NotFound();

            string? userId = User.Claims.FirstOrDefault(c => c.Type == "DatabaseId")?.Value;
            if (userId is null)
                return Unauthorized();

            if (!await _orgRepo.IsAdminOfOrg(id, userId))
                return Forbid();

            return await SaveImage("org", id, file);
        }

        private async Task<ActionResult> SaveImage(string category, string id, IFormFile file)
        {
            Console.WriteLine(file.FileName);
            var contentTokens = file.ContentType.Split("/");
            if (contentTokens[0] != "image")
                return BadRequest("Only image files are supported");

            if (file.Headers.ContentLength > _maxContentSize)
                return BadRequest($"Cannot upload an image larger than 5MB");

            try
            {
                await _imageService.SaveImage(file.OpenReadStream(), id, category);
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
