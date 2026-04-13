using MongoDB.Driver;
using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.Workers;

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
builder.Services.AddScoped<ITheMovieDBService, TheMovieDBService>();
builder.Services.AddScoped<IMediaCacheRepository, MediaCacheRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

builder.Services.AddHostedService<TmdbCacheSyncWorker>();

var app = builder.Build();

// 4. MIDDLEWARE 
app.UseCors("OpenPolicy");
app.MapControllers();

// Endpoint 
app.MapGet("/", () => "API de MaratonHub operativa"); 
app.Run();