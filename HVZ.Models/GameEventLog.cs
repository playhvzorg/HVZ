using NodaTime;
namespace HVZ.Models;

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
        return this.GameEvent switch
        {
            GameEvent.GameCreated => $"{this.Timestamp.ToString()} Game {this.AdditionalInfo["name"]} created by {this.UserId}",
            GameEvent.PlayerJoined => $"{this.Timestamp.ToString()} User {this.UserId} joined the game",
            GameEvent.PlayerLeft => $"{this.Timestamp.ToString()} User {this.UserId} left the game",
            GameEvent.Tag => $"{this.Timestamp.ToString()} User {this.UserId} tagged user {this.AdditionalInfo["tagreciever"]}",
            GameEvent.PlayerRoleChangedByMod => $"{this.Timestamp.ToString()} User {this.UserId} was set to {(Player.GameRole)this.AdditionalInfo["role"]} by {this.AdditionalInfo["modid"]}",
            GameEvent.ActiveStatusChanged => $"{this.Timestamp.ToString()} Game set to {((bool)this.AdditionalInfo["state"] ? "active" : "inactive")} by {this.UserId}",
            _ => $"{this.Timestamp.ToString()} Unrecognized event: {this.GameEvent.ToString()} user: {this.UserId} {this.AdditionalInfo.ToString()}"
        };
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