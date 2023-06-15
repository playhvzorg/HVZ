namespace HVZ.Web.Shared.Models
{
    public class CreateOrgResult
    {
        public bool Succeeded { get; set; }
        public string? OrgId { get; set; }
        public string? Error { get; set; }
    }
}
