using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using HVZ.Web.Data;
namespace HVZ.Web;
public class Webapp
{
    WebApplication app;
    public Webapp(string[] args)
    {
        var builder = WebApplication.CreateBuilder(
            new WebApplicationOptions()
            {
                ApplicationName = "HVZ.Web"
            }
        );

        // Add services to the container.
        builder.Services.AddRazorPages();
        builder.Services.AddServerSideBlazor();
        builder.Services.AddSingleton<WeatherForecastService>();
        app = builder.Build();
    }

    public Task RunAsnyc()
    {
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

        app.MapBlazorHub();

        app.MapFallbackToPage("/_Host");

        return app.RunAsync();
    }
}



