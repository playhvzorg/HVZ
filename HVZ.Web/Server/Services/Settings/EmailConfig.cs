namespace HVZ.Web.Server.Services.Settings
{
    public class EmailConfig
    {
        public required string SmtpHost { get; set; }
        public int Port { get; set; }
        public required string EmailId { get; set; }
        public string? EmailAlais { get; set; }
        public required string Password { get; set; }
    }
}
