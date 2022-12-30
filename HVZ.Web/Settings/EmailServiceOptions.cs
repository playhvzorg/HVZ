
namespace HVZ.Web.Settings
{
    public class EmailServiceOptions
    {
        public string? SmtpHost { get; set; }
        public int Port { get; set; }
        public string? EmailId { get; set; }
        public string? Password { get; set; }
    }
}