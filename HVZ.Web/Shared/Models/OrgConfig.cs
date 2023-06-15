using HVZ.Persistence.Models;

namespace HVZ.Web.Shared.Models
{
    public class OrgConfig
    {
        public bool RequireProfilePicture { get; set; }
        public bool RequireVerifiedEmail { get; set; }

        public OrgConfig(Organization org)
        {

        }
    }
}
