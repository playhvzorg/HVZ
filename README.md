[playhvz.org](https://playhvz.org)
***

This is a website dedicated to managing Humans Vs Zombies games. Originally created by the computer science department of UNCC's Charlotte Outdoor Gaming club, this project supports both a standalone website and optional discord integration. 

Supported features:
* Organiazations
    * User created orgs can create and host games.
    * Orgs have administrators and moderators to manage users throughout the game.

* Games
    * Games may be started, paused, and stopped by org admins.
    * Users will have an unique ID randomly generated for them.
    * Users joining will be set to human or zombie based on the game settings.
    * OZ support
        * Users can decide if they want to be an OZ, and when the game starts a few of these users will be selected and notified. The amount of users is configurable, and can be entirely disabled by org admins.
        * OZ tags do not show who the OZ is.
        * The OZ can choose to be a zombie or human after getting a set number of tags.
* Discord integration

***
# Setup
## Discord
Create a discord bot and add it to your server [(Instructions here)](https://discordnet.dev/guides/getting_started/first-bot.html)

Configure your DiscordIntegrationSettings. The recommended way is to use `dotnet user-secrets`:<br>
`dotnet user-secrets set "DiscordIntegrationSettings.Token" "<your token here>"`<br>
If you choose to store your credentials unsecurely in `appsettings.json`, DO NOT CHECK THEM INTO GIT. 

Once all of the `DiscordIntegrationSettings` are set, the bot will be active along with the webserver.