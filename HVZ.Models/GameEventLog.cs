namespace HVZ.Models;

using NodaTime;

public record GameEventLog {
    public IDictionary<string, object> AdditionalInfo;
    public GameEvent GameEvent;
    public Instant Timestamp;
    /// <summary>
    ///     The user that caused this event to happen
    /// </summary>
    public string UserId;

    public GameEventLog(GameEvent gameEvent, Instant timestamp, string userid, IDictionary<string, object> additionalinfo = null!)
    {
        GameEvent = gameEvent;
        Timestamp = timestamp;
        UserId = userid;
        AdditionalInfo = additionalinfo;
    }

    public override string ToString()
    {
        return GameEvent switch
        {
            GameEvent.GameCreated => $"{Timestamp.ToString()} Game {AdditionalInfo["name"]} created by {UserId}",
            GameEvent.PlayerJoined => $"{Timestamp.ToString()} User {UserId} joined the game",
            GameEvent.PlayerLeft => $"{Timestamp.ToString()} User {UserId} left the game",
            GameEvent.Tag => $"{Timestamp.ToString()} User {UserId} tagged user {AdditionalInfo["tagreciever"]}",
            GameEvent.PlayerRoleChangedByMod => $"{Timestamp.ToString()} User {UserId} was set to {(Player.GameRole)AdditionalInfo["role"]} by {AdditionalInfo["modid"]}",
            GameEvent.ActiveStatusChanged => $"{Timestamp.ToString()} Game set to {((bool)AdditionalInfo["state"] ? "active" : "inactive")} by {UserId}",
            _ => $"{Timestamp.ToString()} Unrecognized event: {GameEvent.ToString()} user: {UserId} {AdditionalInfo}"
        };
    }
}

public enum GameEvent {
    GameCreated,
    PlayerJoined,
    PlayerLeft,
    Tag,
    PlayerRoleChangedByMod,
    ActiveStatusChanged
}