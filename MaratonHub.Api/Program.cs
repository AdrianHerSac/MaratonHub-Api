using MongoDB.Driver;
using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.Workers;
using MaratonHub.Api.Users.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<ITheMovieDBService, TheMovieDBService>();
builder.Services.AddScoped<IMediaCacheRepository, MediaCacheRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// builder.Services.AddHostedService<TmdbCacheSyncWorker>();

var app = builder.Build();

// 4. MIDDLEWARE 
app.UseCors("OpenPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Endpoint 
app.MapGet("/", () => "API de MaratonHub operativa"); 
app.Run();