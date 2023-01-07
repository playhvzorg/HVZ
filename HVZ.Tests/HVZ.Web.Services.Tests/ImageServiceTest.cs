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

    private string uploadPath = "";
    private string sharedUserAndOrgId = "0";
    private string uniqueUserId = "1";
    private string uniqueOrgId = "2";

    private async Task<SKBitmap> OpenFileAsBitmap(string path)
    {
        SKBitmap img;
        await using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
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
        uploadPath = Path.Combine(Path.GetTempPath(), "HVZ.Test", "images");
        Directory.CreateDirectory(uploadPath);
        // Directory.CreateDirectory(Path.Combine(uploadPath, "users"));
        // Directory.CreateDirectory(Path.Combine(uploadPath, "orgs"));
        IOptions<ImageServiceOptions> options = Mock.Of<IOptions<ImageServiceOptions>>(
            x => x.Value == new ImageServiceOptions { UploadPath = uploadPath });
        imageService = new ImageService(options);

        mockBrowserFile.Setup(file => file.OpenReadStream(4096 * 4096 * 32, default(CancellationToken)))
            .Returns(
                // new FileStream($"../../../resources/0.png", FileMode.Open)
                () =>
                {
                    var fs = new FileStream($"../../../resources/0.png", FileMode.Open);
                    fs.Seek(0, SeekOrigin.Begin);
                    return fs;
                }
            );
        mockBrowserFile.Setup(file => file.ContentType).Returns("image/png");

        // Create the test images

        await imageService.SaveUserImage(mockBrowserFile.Object, sharedUserAndOrgId);
        await imageService.SaveUserImage(mockBrowserFile.Object, uniqueUserId);
        await imageService.SaveOrgImage(mockBrowserFile.Object, sharedUserAndOrgId);
        await imageService.SaveOrgImage(mockBrowserFile.Object, uniqueOrgId);

    }

    // [OneTimeTearDown]
    public void Teardown()
    {
        // Delete the test output
        File.Delete(Path.Combine(uploadPath, "users", "0.png"));
        File.Delete(Path.Combine(uploadPath, "users", "0_thumbnail_64.jpeg"));
        File.Delete(Path.Combine(uploadPath, "users", "0_thumbnail_128.jpeg"));
        File.Delete(Path.Combine(uploadPath, "users", "0_thumbnail_256.jpeg"));

        File.Delete(Path.Combine(uploadPath, "users", "1.png"));
        File.Delete(Path.Combine(uploadPath, "users", "1_thumbnail_64.jpeg"));
        File.Delete(Path.Combine(uploadPath, "users", "1_thumbnail_128.jpeg"));
        File.Delete(Path.Combine(uploadPath, "users", "1_thumbnail_256.jpeg"));

        File.Delete(Path.Combine(uploadPath, "orgs", "0.png"));
        File.Delete(Path.Combine(uploadPath, "orgs", "0_thumbnail_64.jpeg"));
        File.Delete(Path.Combine(uploadPath, "orgs", "0_thumbnail_128.jpeg"));
        File.Delete(Path.Combine(uploadPath, "orgs", "0_thumbnail_256.jpeg"));

        File.Delete(Path.Combine(uploadPath, "orgs", "2.png"));
        File.Delete(Path.Combine(uploadPath, "orgs", "2_thumbnail_64.jpeg"));
        File.Delete(Path.Combine(uploadPath, "orgs", "2_thumbnail_128.jpeg"));
        File.Delete(Path.Combine(uploadPath, "orgs", "2_thumbnail_256.jpeg"));
    }

    [Test]
    public void Test_CorrectFileNames()
    {
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "0.png")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "0_thumbnail_64.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "0_thumbnail_128.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "0_thumbnail_256.jpeg")), Is.True);

        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "1.png")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "1_thumbnail_64.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "1_thumbnail_128.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "users", "1_thumbnail_256.jpeg")), Is.True);

        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "0.png")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "0_thumbnail_64.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "0_thumbnail_128.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "0_thumbnail_256.jpeg")), Is.True);

        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "2.png")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "2_thumbnail_64.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "2_thumbnail_128.jpeg")), Is.True);
        Assert.That(File.Exists(Path.Combine(uploadPath, "orgs", "2_thumbnail_256.jpeg")), Is.True);
    }

    [Test]
    public void Test_SaveNonImageThrowsError()
    {
        IBrowserFile textFile = Mock.Of<IBrowserFile>(
            x => x.ContentType == "text/json"
        );
        Assert.ThrowsAsync<ArgumentException>(async () => await imageService.SaveUserImage(textFile, "1234"));
        Assert.ThrowsAsync<ArgumentException>(async () => await imageService.SaveOrgImage(textFile, "1234"));

    }

    [TestCase("users")]
    [TestCase("orgs")]
    public async Task Test_SmallThumbnailSize(string category)
    {
        var smThumbnail = await OpenFileAsBitmap(Path.Combine(uploadPath, category, "0_thumbnail_64.jpeg"));
        Assert.That(smThumbnail.Width == (int)ImageService.ImageSize.SMALL);
        Assert.That(smThumbnail.Height == (int)ImageService.ImageSize.SMALL);
        smThumbnail.Dispose();
    }

    [TestCase("users")]
    [TestCase("orgs")]
    public async Task Test_MediumThumbnilSize(string category)
    {
        var mdThumbnail = await OpenFileAsBitmap(Path.Combine(uploadPath, category, "0_thumbnail_128.jpeg"));
        Assert.That(mdThumbnail.Width == (int)ImageService.ImageSize.MEDIUM);
        Assert.That(mdThumbnail.Height == (int)ImageService.ImageSize.MEDIUM);
        mdThumbnail.Dispose();
    }

    [TestCase("users")]
    [TestCase("orgs")]
    public async Task Test_LargeTumbnailSize(string category)
    {
        var lgThumbnail = await OpenFileAsBitmap(Path.Combine(uploadPath, category, "0_thumbnail_256.jpeg"));
        Assert.That(lgThumbnail.Width == (int)ImageService.ImageSize.LARGE);
        Assert.That(lgThumbnail.Height == (int)ImageService.ImageSize.LARGE);
        lgThumbnail.Dispose();
    }

    [TestCase(ImageService.ImageSize.SMALL)]
    [TestCase(ImageService.ImageSize.MEDIUM)]
    [TestCase(ImageService.ImageSize.LARGE)]
    public void Test_GetUserThumbnailResourcePath(ImageService.ImageSize size)
    {
        string sharedIdResPath = imageService.GetUserThumbnailResourceLink("0", size);
        Assert.That(sharedIdResPath == $"images/users/0_thumbnail_{(int)size}.jpeg");

        string uniqueIdResPath = imageService.GetUserThumbnailResourceLink("1", size);
        Assert.That(uniqueIdResPath == $"images/users/1_thumbnail_{(int)size}.jpeg");
    }

    [TestCase(ImageService.ImageSize.SMALL)]
    [TestCase(ImageService.ImageSize.MEDIUM)]
    [TestCase(ImageService.ImageSize.LARGE)]
    public void Test_GetOrgThumbnailResourcePath(ImageService.ImageSize size)
    {
        string sharedIdResPath = imageService.GetOrgThumbnailResourceLink("0", size);
        Assert.That(sharedIdResPath == $"images/orgs/0_thumbnail_{(int)size}.jpeg");

        string uniqueIdResPath = imageService.GetOrgThumbnailResourceLink("2", size);
        Assert.That(uniqueIdResPath == $"images/orgs/2_thumbnail_{(int)size}.jpeg");
    }

    [Test]
    public void Test_UserHasSavedImage()
    {
        Assert.That(imageService.HasUploadedUserImage(sharedUserAndOrgId), Is.True);
        Assert.That(imageService.HasUploadedUserImage(uniqueUserId), Is.True);

        Assert.That(imageService.HasUploadedUserImage(uniqueOrgId), Is.False);
        Assert.That(imageService.HasUploadedUserImage("1234"), Is.False);
    }

    [Test]
    public void Test_OrgHasSavedImage()
    {
        Assert.That(imageService.HasUploadedOrgImage(sharedUserAndOrgId), Is.True);
        Assert.That(imageService.HasUploadedOrgImage(uniqueOrgId), Is.True);

        Assert.That(imageService.HasUploadedOrgImage(uniqueUserId), Is.False);
        Assert.That(imageService.HasUploadedOrgImage("1234"), Is.False);
    }

}