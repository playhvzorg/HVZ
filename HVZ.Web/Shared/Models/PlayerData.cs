using HVZ.Persistence.Models;

namespace HVZ.Web.Shared.Models
{
    public class PlayerData
    {
        public required UserData User { get; set; }
        public required string GameId { get; set; }
        public required int Tags { get; set; }
        public required Player.gameRole Role { get; set; }
    }
}
