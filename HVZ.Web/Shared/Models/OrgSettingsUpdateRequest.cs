namespace HVZ.Web.Shared.Models
{
    public class OrgSettingsUpdateRequest
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? RequireProfilePicture { get; set; }
        public bool? RequireVerifiedEmail { get; set; }
    }
}
