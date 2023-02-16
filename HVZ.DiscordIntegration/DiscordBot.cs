namespace HVZ.DiscordIntegration;
using Discord.WebSocket;
using Discord.Interactions;
using Discord;
public class DiscordBot
{
    private string token = null!;
    private DiscordSocketClient client = null!;
    private InteractionService interactionService = null!;
    private bool initialized;
    public static DiscordBot Instance => new DiscordBot();
    private DiscordBot() { }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine($"{msg.Severity} {msg.Message}");
        return Task.CompletedTask;
    }

    public void Init(string discordToken, IServiceProvider serviceProvider)
    {
        if (initialized)
            throw new InvalidOperationException("DiscordBot is already initialized");
        this.token = discordToken;
        client = new();
        interactionService = new(client);
        client.Log += Log;
        interactionService.AddModulesAsync(typeof(DiscordBot).Assembly, serviceProvider);
        initialized = true;
    }

    public async Task Run()
    {
        if (!initialized)
            throw new InvalidOperationException("DiscordBot has not yet been initialized");
        await client.LoginAsync(TokenType.Bot, token);
        await client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }
}
