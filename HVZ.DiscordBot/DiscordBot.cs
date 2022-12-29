namespace HVZ.DiscordBot;
using Discord.WebSocket;
using Discord;
public class DiscordBot
{
    private string token;
    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }
    private DiscordSocketClient _client;

    public DiscordBot(string token)
    {
        this.token = token;
        _client = new DiscordSocketClient();
        _client.Log += Log;
    }

    public async Task Run()
    {
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        // Block this task until the program is closed.
        await Task.Delay(-1);
    }
}
