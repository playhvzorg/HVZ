namespace HVZ.DiscordIntegration.Modules;
using Discord.Interactions;

public class HVZ : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("Website", "Provides the link to the website")]
    public async Task URLCommand() => await RespondAsync("https://playhvz.org");
}