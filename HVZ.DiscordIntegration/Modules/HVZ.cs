namespace HVZ.DiscordIntegration.Modules;
using Discord.Interactions;

public class Hvz : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("Website", "Provides the link to the website")]
    public async Task UrlCommand() => await RespondAsync("https://playhvz.org");
}