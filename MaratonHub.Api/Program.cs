using MongoDB.Driver;
using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.Workers;
using MaratonHub.Api.Users.Repositories;
using MaratonHub.Api.Common;
using MaratonHub.Api.Groups.Repositories;
using MaratonHub.Api.Groups.Hubs;
using MaratonHub.Api.Notifications.Repositories;
using MaratonHub.Api.Notifications.Services;
using MaratonHub.Api.Notifications.Hubs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Desactivar el mapeo automático de claims JWT para que se usen los nombres originales
// (evita que "unique_name" se mapee a ClaimTypes.Name URI larga)
System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

// CORS - Permitir tanto localhost como el frontend en produccion
var allowedOrigins = new[]
{
    "http://localhost:4200",
    "https://maratonhub.vercel.app",
    "https://maratonhub-web.vercel.app"
};

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRPolicy", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// MONGODB ATLAS
builder.Services.AddSingleton<IMongoClient>(sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config["MongoDbSettings__ConnectionString"]
                           ?? config.GetSection("MongoDbSettings")["ConnectionString"];

    if (string.IsNullOrEmpty(connectionString))
    {
        throw new Exception("MongoDB Connection String is missing!");
    }

    var settings = MongoClientSettings.FromUrl(new MongoUrl(connectionString));
    settings.ConnectTimeout = TimeSpan.FromSeconds(10);
    settings.ServerSelectionTimeout = TimeSpan.FromSeconds(10);

    return new MongoClient(settings);
});

builder.Services.AddScoped(sp => {
    var config = sp.GetRequiredService<IConfiguration>();
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = config["MongoDbSettings__DatabaseName"] ?? "MaratonHub";
    return client.GetDatabase(dbName);
});

// REGISTRO DE SERVICIOS
builder.Services.AddControllers();

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"]!;

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            RoleClaimType = "role",
            NameClaimType = "unique_name"
        };

        // Configurar SignalR para recibir el token JWT desde query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });
builder.Services.AddAuthorization();

// SignalR
builder.Services.AddSignalR();

// DI - Servicios existentes
builder.Services.AddScoped<ITheMovieDBService, TheMovieDBService>();
builder.Services.AddScoped<IMediaCacheRepository, MediaCacheRepository>();
builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// DI - Grupos
builder.Services.AddScoped<IGroupRepository, GroupRepository>();
builder.Services.AddScoped<IGroupRatingRepository, GroupRatingRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// DI - Notificaciones
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// builder.Services.AddHostedService<TmdbCacheSyncWorker>();

var app = builder.Build();

// MIDDLEWARE
app.UseCors("SignalRPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

// Endpoint raiz
app.MapGet("/", () => "API de MaratonHub operativa");
app.Run();
