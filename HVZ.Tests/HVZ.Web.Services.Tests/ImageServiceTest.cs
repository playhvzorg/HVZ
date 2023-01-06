using Moq;
using HVZ.Web.Settings;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using SkiaSharp;

namespace HVZ.Web.Services.Tests;

public class ImageServiceTest
{
    private ImageService imageService = null!;
    private Mock<IBrowserFile> mockBrowserFile = new Mock<IBrowserFile>();
    private Mock<IOptions<ImageServiceOptions>> mockServiceOptions = new Mock<IOptions<ImageServiceOptions>>();

    private string uploadPath = "";

    private async Task<SKBitmap> OpenFileAsBitmap(string path)
    {
        SKBitmap img;
        await using FileStream fs = new FileStream(path, FileMode.Open);
        using (var stream = new SKManagedStream(fs))
        {
            img = SKBitmap.Decode(stream);
        }
        fs.Close();
        return img;
    }

    [OneTimeSetUp]
    public async Task SetUp()
    {
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "HVZ.Test", "images"));
        uploadPath = Path.Combine(Path.GetTempPath(), "HVZ.Test", "images");
        mockServiceOptions.Setup(opt => opt.Value).Returns(new ImageServiceOptions { UploadPath = uploadPath});
        imageService = new ImageService(mockServiceOptions.Object);
        
        mockBrowserFile.Setup(file => file.OpenReadStream(4096 * 4096 * 32, default(CancellationToken))).Returns(new FileStream($"../../../resources/0.png", FileMode.Open, FileAccess.Read));
        mockBrowserFile.Setup(file => file.ContentType).Returns("image/png");
        // Create the test images
        await imageService.SaveImage(mockBrowserFile.Object, "0");

    }

    [OneTimeTearDown]
    public void Teardown()
    {
        // Delete the images test images
        File.Delete(Path.Combine(uploadPath, "0.png"));
        File.Delete(Path.Combine(uploadPath, "0_thumbnail_64.jpeg"));
        File.Delete(Path.Combine(uploadPath, "0_thumbnail_128.jpeg"));
        File.Delete(Path.Combine(uploadPath, "0_thumbnail_256.jpeg"));
    }

    [Test]
    public void Test_CorrectFileCount()
    {
        string[] filesInDirectory = Directory.GetFiles(uploadPath);
        Assert.That(filesInDirectory.Length == 4);
    }

    [Test]
    public void Test_CorrectFileNames()
    {
        Assert.That(File.Exists(Path.Combine(uploadPath, "0.png")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "0_thumbnail_64.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "0_thumbnail_128.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "0_thumbnail_256.jpeg")), Is.True);
    }

    [Test]
    public async Task Test_SmallThumbnailSize()
    {
        var smThumbnail = await OpenFileAsBitmap(Path.Combine(uploadPath, "0_thumbnail_64.jpeg"));
        Assert.That(smThumbnail.Width == 64);
        Assert.That(smThumbnail.Height == 64);
        smThumbnail.Dispose();
    }

    [Test]
    public async Task Test_MediumThumbnilSize()
    {
        var mdThumbnail = await OpenFileAsBitmap(Path.Combine(uploadPath, "0_thumbnail_128.jpeg"));
        Assert.That(mdThumbnail.Width == 128);
        Assert.That(mdThumbnail.Height == 128);
        mdThumbnail.Dispose();
    }

    [Test]
    public async Task Test_LargeTumbnailSize()
    {
        var lgThumbnail = await OpenFileAsBitmap(Path.Combine(uploadPath, "0_thumbnail_256.jpeg"));
        Assert.That(lgThumbnail.Width == 256);
        Assert.That(lgThumbnail.Height == 256);
        lgThumbnail.Dispose();
    }

}