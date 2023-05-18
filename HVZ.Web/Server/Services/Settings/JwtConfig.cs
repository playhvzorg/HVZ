namespace HVZ.Web.Server.Services.Settings
{
    public class JwtConfig
    {
        public string JwtSecurityKey { get; set; } = string.Empty;
        public string JwtIssuer { get; set; } = string.Empty;
        public string JwtAudience { get; set; } = string.Empty;
        public int JwtExpiryInDays { get; set; }
    }
}
