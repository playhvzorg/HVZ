namespace HVZ.Web.Server.Services.Settings
{
    public class ImageConfig
    {
        /// <summary>
        /// Directory to store user uploaded images. If left unspecified, defaults to the system ApplicationData directory.
        /// </summary>
        /// <remarks>
        /// Must be an absolute folder path.<br />
        /// It is recommended to not use wwwroot as that can introduce security vulnarabilities when working with user generated content.
        /// </remarks>
        public string? ImageDirectory { get; set; }
    }
}
