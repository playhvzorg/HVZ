namespace HVZ.DiscordIntegration;
using Discord.WebSocket;
using Discord.Interactions;
using Discord;
public class DiscordBot
{
    private string token = null!;
    private DiscordSocketClient _client = null!;
    private InteractionService _interactionService = null!;
    private bool initialized = false;
    public static DiscordBot instance => new DiscordBot();
    private DiscordBot() { }
    private Task Log(LogMessage msg)
    {
        Console.WriteLine($"{msg.Severity} {msg.Message}");
        return Task.CompletedTask;
    }

    public void init(string token, IServiceProvider serviceProvider)
    {
        if (initialized)
            throw new InvalidOperationException("DiscordBot is already initialized");
        this.token = token;
        _client = new();
        _interactionService = new(_client);
        _client.Log += Log;
        _interactionService.AddModulesAsync(typeof(DiscordBot).Assembly, serviceProvider);
        initialized = true;
    }

    public async Task Run()
    {
        if (!initialized)
            throw new InvalidOperationException("DiscordBot has not yet been initialized");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }
}
