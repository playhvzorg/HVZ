using Microsoft.AspNetCore.Identity;
using HVZ.Web.Data;
using HVZ.Persistence;
using HVZ.Persistence.MongoDB.Repos;
using HVZ.Web.Identity;
using HVZ.Web.Identity.Models;
using HVZ.Web.Settings;
using HVZ.Web.Services;
using MongoDB.Driver;

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

        var discordIntegrationSettings = builder.Configuration.GetSection(nameof(DiscordIntegrationSettings))
            .Get<DiscordIntegrationSettings>();
        builder.Services.AddSingleton(discordIntegrationSettings!);

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