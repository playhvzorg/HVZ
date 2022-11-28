using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Auth0.AspNetCore.Authentication;
using HVZ.Web.Data;
using HVZ.Persistence;
using HVZ.Persistence.MongoDB.Repos;
using MongoDB.Driver;
using HVZ.Models;
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

        builder.Services
            .AddAuth0WebAppAuthentication( options => {
                options.Domain = builder.Configuration["Auth0:Domain"];
                options.ClientId = builder.Configuration["Auth0:ClientId"];
                options.Scope = "openid profile email";
            });
        
        #region Persistence
        var mongoClient = new MongoClient(
            builder.Configuration["DatabaseSettings:ConnectionString"]
        );

        var mongoDatabase = mongoClient.GetDatabase(
            builder.Configuration["DatabaseSettings:DatabaseName"]
        );

        builder.Services.AddSingleton<IGameRepo>(new GameRepo(mongoDatabase, SystemClock.Instance));

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
        app.MapFallbackToPage("/_Host");

        app.Run();
    }

}



