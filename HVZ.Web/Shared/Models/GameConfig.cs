using HVZ.Persistence.Models;

namespace HVZ.Web.Shared.Models
{
    public class GameConfig
    {
        public Player.gameRole DefaultRole { get; set; }
        public int OzMaxTags { get; set; }
        public string? OzPassword { get; set; }

        public GameConfig(Game game)
        {
            DefaultRole = game.DefaultRole;
            OzMaxTags = game.OzMaxTags;
            OzPassword = game.OzPassword;
        }
    }
}
