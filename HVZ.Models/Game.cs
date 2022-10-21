using System.Collections.Generic;
using NodaTime;
namespace HVZ.Models;

public class Game
{
    /// <summary>
    /// Unique identifier of this specific game
    /// </summary>
    public string Id { get; init; }
    /// <summary>
    /// ID of the user who created this game
    /// </summary>
    public string UserId { get; init; }
    public Instant CreatedAt { get; init; }
    public enum GameState
    {
        registration,
        play,
        finished,
    }
    /// <summary>
    /// current state of the game
    /// </summary>
    public GameState State { get; init; }
    /// <summary>
    /// Users who are a human in this game
    /// </summary>
    public HashSet<User> Humans { get; init; }
    /// <summary>
    /// Users who are zombies in this game
    /// </summary>
    public HashSet<User> Zombies { get; init; }
    /// <summary>
    /// Users who are OZs in this game
    /// </summary>
    /// <value></value>
    public HashSet<User> Ozs { get; init; }
    /// <summary>
    /// All Users in this game
    /// </summary>
    /// <value></value>
    public HashSet<User> Players
    {
        get
        {
            return Humans.Concat(Zombies).Concat(Ozs).ToHashSet<User>();
        }
    }
    /// <summary>
    /// Users who are Zombies or OZs
    /// </summary>
    /// <value></value>
    public HashSet<User> ZombiesAndOzs
    {
        get
        {
            return Zombies.Concat(Ozs).ToHashSet<User>();
        }
    }

    /// <summary>
    /// The role to put new people in when they join a game
    /// </summary>
    public enum DefaultPlayerRole
    {
        Human,
        Zombie
    }
    /// <summary>
    /// The default role to put new people in when they join this game
    /// </summary>
    public DefaultPlayerRole DefaultRole { get; init; }

    public Game(string id, string userid, Instant createdat, GameState state, HashSet<User> humans, HashSet<User>zombies, HashSet<User> ozs)
    {
        Id = id;
        UserId = userid;
        CreatedAt = createdat;
        State = state;
        DefaultRole = DefaultPlayerRole.Human;
        Humans = humans;
        Zombies = zombies;
        Ozs = ozs;
    }
}