using AspNetCore.Identity.MongoDbCore.Models;
using HVZ.Persistence;
using HVZ.Persistence.Models;
using HVZ.Persistence.MongoDB.Repos;
using HVZ.Web.Server.Hubs;
using HVZ.Web.Server.Identity;
using HVZ.Web.Server.JsonConverters;
using HVZ.Web.Server.Services;
using HVZ.Web.Server.Services.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllersWithViews()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new InstantConverter());
        opts.JsonSerializerOptions.Converters.Add(new EnumConverter<Player.gameRole>());
        opts.JsonSerializerOptions.Converters.Add(new EnumConverter<Game.GameStatus>());
        opts.JsonSerializerOptions.Converters.Add(new EnumConverter<GameEvent>());
        //opts.JsonSerializerOptions.Converters.Add(new EventLogInfoConverter());
    });
builder.Services.AddRazorPages();

#region Generic Options
builder.Services.Configure<WebConfig>(
    builder.Configuration.GetSection(nameof(WebConfig))
);
#endregion

#region Logging

ILogger logger = LoggerFactory.Create(config =>
{
    config.ClearProviders();
    config.AddConsole();
    // TODO: Add file provider
}).CreateLogger("Program");

#endregion

#region Persistence

var mongoConfig = builder.Configuration.GetSection(nameof(MongoConfig)).Get<MongoConfig>();

var mongoClient = new MongoClient(
    mongoConfig?.ConnectionString
);

var mongoDatabase = mongoClient.GetDatabase(
    mongoConfig?.DatabaseName
);

builder.Services.AddSingleton<IMongoDatabase>(mongoDatabase);

IGameRepo gameRepo = new GameRepo(
    mongoDatabase,
    NodaTime.SystemClock.Instance,
    logger
);
IUserRepo userRepo = new UserRepo(
    mongoDatabase,
    NodaTime.SystemClock.Instance,
    logger
);
IOrgRepo orgRepo = new OrgRepo(
    mongoDatabase,
    NodaTime.SystemClock.Instance,
    userRepo,
    gameRepo,
    logger
);

builder.Services.AddSingleton<IGameRepo>(gameRepo);
builder.Services.AddSingleton<IUserRepo>(userRepo);
builder.Services.AddSingleton<IOrgRepo>(orgRepo);

#endregion

#region Identity

builder.Services.AddIdentity<ApplicationUser, MongoIdentityRole<Guid>>()
    .AddMongoDbStores<ApplicationUser, MongoIdentityRole<Guid>, Guid>(
        mongoConfig?.ConnectionString, mongoConfig?.DatabaseName
    )
    .AddDefaultTokenProviders();

builder.Services
    .AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationClaimsPrincipalFactory>();

var jwtConfig = builder.Configuration.GetSection(nameof(JwtConfig)).Get<JwtConfig>() 
    ?? throw new ArgumentNullException("JwtConfig must be defined in appsettings.json");

builder.Services.AddSingleton(jwtConfig);

builder.Services.AddAuthentication().AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtConfig.JwtIssuer,
        ValidAudience = jwtConfig.JwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.JwtSecurityKey))
    };
});

builder.Services.AddAuthorization();

#endregion

#region Images

builder.Services.Configure<ImageConfig>(
    builder.Configuration.GetSection(nameof(ImageConfig))
);
builder.Services.AddSingleton<ImageService>();

#endregion

#region Discord Integration
// TODO
#endregion

#region Email

builder.Services.Configure<EmailConfig>(
    builder.Configuration.GetSection(nameof(EmailConfig))
);
builder.Services.AddSingleton<EmailService>();

#endregion

#region API Documentation

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "PlayHVZ API",
        Description = "RESTful web API for participaing in and managing HVZ games",
        TermsOfService = new Uri("https://playhvz.org/terms"),
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
});

#endregion

#region SignalR

builder.Services.AddSignalR();
builder.Services.AddResponseCompression(opts =>
{
    opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/octet-stream" });
});

#endregion

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();
app.MapFallbackToFile("index.html");

//app.UseResponseCompression();
app.MapHub<GameHub>("/gamehub");
app.MapHub<OrgHub>("/orghub");

app.Run();
