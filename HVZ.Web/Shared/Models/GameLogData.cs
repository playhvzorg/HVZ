using HVZ.Persistence.Models;
using NodaTime;

namespace HVZ.Web.Shared.Models
{
    public class GameLogData
    {
        public required GameEvent EventType { get; set; }
        public required Instant Timestamp { get; set; }
        public required UserData User { get; set; }
        public IEventLogInfo? EventInfo { get; set; }
    }

    public interface IEventLogInfo
    {

    }

    public class GameCreatedEventLogInfo : IEventLogInfo
    {
        public required string GameName { get; set; }
    }

    public class TagEventLogInfo : IEventLogInfo
    {
        public required UserData TagReceiver { get; set; }
        public required int TaggerTagCount { get; set; }
        public required bool OzTagger { get; set; }
    }

    public class RoleSetEventLogInfo : IEventLogInfo
    {
        public UserData? Instigator { get; set; }
        public required Player.gameRole Role { get; set; }
    }

    public class ActiveStatusChangedEventLogInfo : IEventLogInfo
    {
        public required Game.GameStatus Status { get; set; }
    }
}
