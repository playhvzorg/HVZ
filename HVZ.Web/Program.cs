using Microsoft.AspNetCore.Identity;
using HVZ.Web.Data;
using HVZ.DiscordBot;
using HVZ.Persistence;
using HVZ.Persistence.MongoDB.Repos;
using HVZ.Web.Identity;
using HVZ.Web.Identity.Models;
using HVZ.Web.Settings;
using HVZ.Web.Services;
using MongoDB.Driver;
using NodaTime;

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

        builder.Services.Configure<WebConfig>(
            builder.Configuration.GetSection(
                nameof(WebConfig)
            )
        );

        #endregion

        #region Persistence

        var mongoClient = new MongoClient(
            builder.Configuration["DatabaseSettings:ConnectionString"]
        );

        var mongoDatabase = mongoClient.GetDatabase(
            builder.Configuration["DatabaseSettings:DatabaseName"]
        );

        IGameRepo gameRepo = new GameRepo(mongoDatabase, SystemClock.Instance);
        IUserRepo userRepo = new UserRepo(mongoDatabase, SystemClock.Instance);
        IOrgRepo orgRepo = new OrgRepo(mongoDatabase, SystemClock.Instance, userRepo, gameRepo);

        builder.Services.AddSingleton<IGameRepo>(gameRepo);
        builder.Services.AddSingleton<IUserRepo>(userRepo);
        builder.Services.AddSingleton<IOrgRepo>(orgRepo);

        #endregion

        #region Identity

        var mongoIdentitySettings = builder.Configuration.GetSection(nameof(MongoIdentityConfig)).Get<MongoIdentityConfig>();
        builder.Services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddMongoDbStores<ApplicationUser, ApplicationRole, Guid>
            (
                mongoIdentitySettings?.ConnectionString, mongoIdentitySettings?.Name
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
        if (discordIntegrationSettings is not null &&
            discordIntegrationSettings.Token is not null &&
            discordIntegrationSettings.ClientId is not null &&
            discordIntegrationSettings.ClientSecret is not null)
        {
            var discordBot = new DiscordBot.DiscordBot(discordIntegrationSettings.Token);
            Task.Run(() => discordBot.Run());

            builder.Services.AddAuthentication(opt =>
                opt.RequireAuthenticatedSignIn = true
            ).AddCookie().AddDiscord(x =>
            {
                x.ClientId = discordIntegrationSettings.ClientId;
                x.ClientSecret = discordIntegrationSettings.ClientSecret;
                x.SaveTokens = true;
            }
        );
        }
        #region Email

        builder.Services.Configure<EmailServiceOptions>(
            builder.Configuration.GetSection(
                nameof(EmailServiceOptions)
            )
        );
        builder.Services.AddSingleton<EmailService>();

        #endregion

        builder.Services.AddSingleton<WeatherForecastService>();

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

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

        app.UseEndpoints(endpoints =>
            endpoints.MapDefaultControllerRoute()
        );

        app.MapBlazorHub();
        // app.MapControllers(); // Enable for API
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

}



