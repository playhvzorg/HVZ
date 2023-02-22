namespace HVZ.Web;

using Data;
using Discord.WebSocket;
using DiscordIntegration;
using Identity;
using Identity.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using NodaTime;
using Persistence;
using Persistence.MongoDB.Repos;
using Services;
using Settings;

internal static class Program {
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(
            new WebApplicationOptions
            {
                ApplicationName = "HVZ.Web"
            }
        );

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();

        #region Generic options

        IConfigurationSection webConfig = builder.Configuration.GetSection(nameof(WebConfig));
        builder.Services.Configure<WebConfig>(webConfig);
        builder.Services.AddSingleton(webConfig.Get<WebConfig>()!);

        #endregion

        ILogger logger = LoggerFactory.Create(config => {
                config.ClearProviders();
                config.AddConsole();
            }
        ).CreateLogger("Program");

        #region Persistence

        var mongoConfig = builder.Configuration.GetSection(nameof(MongoConfig)).Get<MongoConfig>();

        var mongoClient = new MongoClient(
            mongoConfig?.ConnectionString
        );

        IMongoDatabase? mongoDatabase = mongoClient.GetDatabase(
            mongoConfig?.Name
        );

        IGameRepo gameRepo = new GameRepo(mongoDatabase, SystemClock.Instance, logger);
        IUserRepo userRepo = new UserRepo(mongoDatabase, SystemClock.Instance, logger);
        IOrgRepo orgRepo = new OrgRepo(mongoDatabase, SystemClock.Instance, userRepo, gameRepo, logger);

        builder.Services.AddSingleton(gameRepo);
        builder.Services.AddSingleton(userRepo);
        builder.Services.AddSingleton(orgRepo);

        #endregion

        #region Identity

        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
            (
                mongoConfig?.ConnectionString, mongoConfig?.Name
            )
            .AddDefaultTokenProviders();
        builder.Services.AddScoped<
            IUserClaimsPrincipalFactory<ApplicationUser>,
            ApplicationClaimsPrincipalFactory
        >();

        #endregion

        #region Images

        builder.Services.Configure<ImageServiceOptions>(
            builder.Configuration.GetSection(
                nameof(ImageServiceOptions)
            )
        );
        builder.Services.AddSingleton<ImageService>();

        #endregion

        #region DiscordIntegration

        var discordIntegrationSettings = builder.Configuration.GetSection(nameof(DiscordIntegrationSettings)).Get<DiscordIntegrationSettings>();
        bool discordIntegrationEnabled = discordIntegrationSettings is not null;
        if (discordIntegrationEnabled)
        {
            var discordBot = DiscordBot.Instance;
            builder.Services.AddSingleton<DiscordSocketClient>();
            builder.Services.AddSingleton(discordBot);
            builder.Services.AddSingleton(discordIntegrationSettings!);
        }

        #endregion

        #region Email

        builder.Services.Configure<EmailServiceOptions>(
            builder.Configuration.GetSection(
                nameof(EmailServiceOptions)
            )
        );
        builder.Services.AddSingleton<EmailService>();

        #endregion

        builder.Services.AddSingleton<WeatherForecastService>();

        WebApplication app = builder.Build();

        if (discordIntegrationEnabled)
        {
            var discordBot = (DiscordBot?)app.Services.GetService(typeof(DiscordBot));
            if (discordBot is null)
                throw new InvalidOperationException("Discord integration is enabled but the service is not configured");
            if (discordIntegrationSettings?.Token is null)
                throw new InvalidOperationException("Discord integration is enabled but the token is not configured");
            discordBot.Init(discordIntegrationSettings?.Token!, app.Services);
            Task.Run(() => discordBot.Run());
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapBlazorHub();
        app.MapControllers();
        app.MapFallbackToPage("/_Host");

        app.Run();
    }
}