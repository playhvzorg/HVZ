using Microsoft.AspNetCore.Identity;
using HVZ.Web.Data;
using HVZ.DiscordIntegration;
using HVZ.Persistence;
using HVZ.Persistence.MongoDB.Repos;
using HVZ.Web.Identity;
using HVZ.Web.Identity.Models;
using HVZ.Web.Settings;
using HVZ.Web.Services;
using MongoDB.Driver;
using NodaTime;
using Discord.WebSocket;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace HVZ.Web;
internal static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions()
            {
                ApplicationName = "HVZ.Web",
            }
        );

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddHttpClient();
        builder.Services.AddHttpContextAccessor();

        #region Generic options

        var webConfig = builder.Configuration.GetSection(nameof(WebConfig));
        builder.Services.Configure<WebConfig>(webConfig);
        builder.Services.AddSingleton<WebConfig>(webConfig.Get<WebConfig>()!);

        #endregion

        ILogger logger = LoggerFactory.Create(config =>
            {
                config.ClearProviders();
                config.AddConsole();
            }
        ).CreateLogger("Program");

        #region Persistence

        var mongoConfig = builder.Configuration.GetSection(nameof(MongoConfig)).Get<MongoConfig>();

        var mongoClient = new MongoClient(
            mongoConfig?.ConnectionString
        );

        var mongoDatabase = mongoClient.GetDatabase(
            mongoConfig?.DatabaseName
        );

        IGameRepo gameRepo = new GameRepo(mongoDatabase, NodaTime.SystemClock.Instance, logger);
        IUserRepo userRepo = new UserRepo(mongoDatabase, NodaTime.SystemClock.Instance, logger);
        IOrgRepo orgRepo = new OrgRepo(mongoDatabase, NodaTime.SystemClock.Instance, userRepo, gameRepo, logger);

        builder.Services.AddSingleton<IGameRepo>(gameRepo);
        builder.Services.AddSingleton<IUserRepo>(userRepo);
        builder.Services.AddSingleton<IOrgRepo>(orgRepo);

        #endregion

        #region Identity

        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
            (
                mongoConfig?.ConnectionString, mongoConfig?.DatabaseName
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
            var discordBot = DiscordBot.instance;
            builder.Services.AddSingleton<DiscordSocketClient>();
            builder.Services.AddSingleton<DiscordBot>(discordBot);
            builder.Services.AddSingleton<DiscordIntegrationSettings>(discordIntegrationSettings!);
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

        var app = builder.Build();

        if (discordIntegrationEnabled)
        {
            DiscordBot? discordBot = (DiscordBot?)app.Services.GetService(typeof(DiscordBot));
            if (discordBot is null)
                throw new InvalidOperationException("Discord integration is enabled but the service is not configured");
            if (discordIntegrationSettings?.Token is null)
                throw new InvalidOperationException("Discord integration is enabled but the token is not configured");
            discordBot.init(discordIntegrationSettings?.Token!, app.Services);
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