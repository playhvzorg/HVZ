using NodaTime;
namespace HVZ.Persistence.Models;

public record GameEventLog
{
    public GameEvent GameEvent;
    public Instant Timestamp;
    /// <summary>
    /// The user that caused this event to happen
    /// </summary>
    public string UserId;
    public IDictionary<string, object> AdditionalInfo;
    public GameEventLog(GameEvent gameEvent, Instant timestamp, string userid, IDictionary<string, object> additionalinfo = null!)
    {
        GameEvent = gameEvent;
        Timestamp = timestamp;
        UserId = userid;
        AdditionalInfo = additionalinfo;
    }
    public override string ToString()
    {
        switch (this.GameEvent)
        {
            case GameEvent.GameCreated:
                return $"{this.Timestamp.ToString()} Game {this.AdditionalInfo["name"]} created by {this.UserId}";
            case GameEvent.PlayerJoined:
                return $"{this.Timestamp.ToString()} User {this.UserId} joined the game";
            case GameEvent.PlayerLeft:
                return $"{this.Timestamp.ToString()} User {this.UserId} left the game";
            case GameEvent.Tag:
                return $"{this.Timestamp.ToString()} User {this.UserId} tagged user {this.AdditionalInfo["tagreciever"]}";
            case GameEvent.PlayerRoleChangedByMod:
                return $"{this.Timestamp.ToString()} User {this.UserId} was set to {(Player.gameRole)this.AdditionalInfo["role"]} by {this.AdditionalInfo["modid"]}";
            case GameEvent.ActiveStatusChanged:
                return $"{this.Timestamp.ToString()} Game set to {((bool)this.AdditionalInfo["state"] ? "active" : "inactive")} by {this.UserId}";
            default:
                return $"{this.Timestamp.ToString()} Unrecognized event: {this.GameEvent.ToString()} user: {this.UserId} {this.AdditionalInfo.ToString()}";
        }
    }
}
public enum GameEvent
{
    GameCreated,
    PlayerJoined,
    PlayerLeft,
    Tag,
    PlayerRoleChangedByMod,
    ActiveStatusChanged,
}