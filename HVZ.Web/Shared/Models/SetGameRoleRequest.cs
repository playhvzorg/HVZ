using HVZ.Persistence.Models;

namespace HVZ.Web.Shared.Models
{
    public class SetGameRoleRequest
    {
        public required string UserId { get; set; }
        public required Player.gameRole Role { get; set; }
    }
}
