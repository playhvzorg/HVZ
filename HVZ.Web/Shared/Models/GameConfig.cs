using HVZ.Persistence.Models;
using System.Text.Json.Serialization;

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

        [JsonConstructor]
        public GameConfig(Player.gameRole defaultRole, int ozMaxTags, string? ozPassword)
        {
            DefaultRole = defaultRole;
            OzMaxTags = ozMaxTags;
            OzPassword = ozPassword;
        }
    }
}
