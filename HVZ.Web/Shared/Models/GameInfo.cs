using HVZ.Persistence.Models;
using NodaTime;

namespace HVZ.Web.Shared.Models
{
    public class GameInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public Instant? StartedAt { get; set; }
        public Instant? EndedAt { get; set; }
        public bool IsActive { get; set; }
        public bool IsCurrent { get; set; }

        public GameInfo(Game game)
        {
            Name = game.Name;
            Id = game.Id;
            StartedAt = game.StartedAt;
            EndedAt = game.EndedAt;
            IsActive = game.IsActive;
            IsCurrent = game.IsCurrent;
        }
    }
}
