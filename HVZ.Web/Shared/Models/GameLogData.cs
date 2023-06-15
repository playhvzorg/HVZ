using HVZ.Persistence.Models;
using NodaTime;
using System.Text.Json.Serialization;

namespace HVZ.Web.Shared.Models
{

    [JsonDerivedType(typeof(GameCreatedLogData), typeDiscriminator: "gameCreated")]
    [JsonDerivedType(typeof(PlayerJoinedEventLogData), typeDiscriminator: "playerJoined")]
    [JsonDerivedType(typeof(PlayerLeftEventLogData), typeDiscriminator: "playerLeft")]
    [JsonDerivedType(typeof(TagEventLogData), typeDiscriminator: "tag")]
    [JsonDerivedType(typeof(RoleSetEventLogData), typeDiscriminator: "roleSet")]
    [JsonDerivedType(typeof(ActiveStatusChangedEventLogData), typeDiscriminator: "gameStatusChanged")]
    [JsonDerivedType(typeof(GameStartedEventLogData), typeDiscriminator: "gameStarted")]
    public abstract class GameLogData
    {
        public required Instant Timestamp { get; set; }
        public required UserData User { get; set; }
        public abstract GameEvent EventType { get; }
    }

    public class GameCreatedLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.GameCreated;
        public required string GameName { get; set; }
    }

    public class TagEventLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.Tag;
        public required UserData TagReceiver { get; set; }
        public required int TaggerTagCount { get; set; }
        public required bool OzTagger { get; set; }
    }

    public class RoleSetEventLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.PlayerRoleChangedByMod;
        public UserData? Instigator { get; set; }
        public required Player.gameRole Role { get; set; }
    }

    public class ActiveStatusChangedEventLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.ActiveStatusChanged;
        public required Game.GameStatus Status { get; set; }
    }

    public class PlayerJoinedEventLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.PlayerJoined;
    }

    public class PlayerLeftEventLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.PlayerLeft;
    }

    public class GameStartedEventLogData : GameLogData
    {
        public override GameEvent EventType => GameEvent.GameStarted;
    }

}
