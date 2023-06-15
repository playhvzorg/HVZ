using Blazored.LocalStorage;
using HVZ.Persistence.Models;
using HVZ.Web.Client;
using HVZ.Web.Client.Interfaces;
using HVZ.Web.Client.Services;
using HVZ.Web.Server.JsonConverters;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using System.Text.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
jsonSerializerOptions.Converters.Add(new InstantConverter());
jsonSerializerOptions.Converters.Add(new EnumConverter<Player.gameRole>());
jsonSerializerOptions.Converters.Add(new EnumConverter<Game.GameStatus>());
jsonSerializerOptions.Converters.Add(new EnumConverter<GameEvent>());

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped(sp => jsonSerializerOptions);
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IOrgService, OrgService>();
builder.Services.AddMudServices();
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
