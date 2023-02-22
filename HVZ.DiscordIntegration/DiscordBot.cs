namespace HVZ.DiscordIntegration;

using Discord;
using Discord.Interactions;
using Discord.WebSocket;

public class DiscordBot {
    private DiscordSocketClient client = null!;
    private bool initialized;
    private InteractionService interactionService = null!;
    private string token = null!;
    private DiscordBot() { }
    public static DiscordBot Instance => new();

    private Task Log(LogMessage msg)
    {
        Console.WriteLine($"{msg.Severity} {msg.Message}");
        return Task.CompletedTask;
    }

    public void Init(string discordToken, IServiceProvider serviceProvider)
    {
        if (initialized)
            throw new InvalidOperationException("DiscordBot is already initialized");
        token = discordToken;
        client = new DiscordSocketClient();
        interactionService = new InteractionService(client);
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