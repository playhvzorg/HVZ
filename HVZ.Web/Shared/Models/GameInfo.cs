using HVZ.Persistence.Models;
using NodaTime;
using System.Text.Json.Serialization;

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
        public Game.GameStatus Status { get; set;}

        public GameInfo(Game game)
        {
            Name = game.Name;
            Id = game.Id;
            StartedAt = game.StartedAt;
            EndedAt = game.EndedAt;
            IsActive = game.IsActive;
            IsCurrent = game.IsCurrent;
            Status = game.Status;
        }

        [JsonConstructor]
        public GameInfo(string name, string id, Instant? startedAt, Instant? endedAt, bool isActive, bool isCurrent, Game.GameStatus status)
        {
            Name = name;
            Id = id;
            StartedAt = startedAt;
            EndedAt = endedAt;
            IsActive = isActive;
            IsCurrent = isCurrent;
            Status = status;
        }
    }
}
