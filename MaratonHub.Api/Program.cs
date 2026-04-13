using MongoDB.Driver;
using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using System.Security.Authentication;
using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURACIÓN DE CORS (Antes de Build) ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("OpenPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- 2. CONFIGURACIÓN DE MONGODB ATLAS ---
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");

builder.Services.AddSingleton<IMongoClient>(sp => 
{
    // Render usará la Variable de Entorno que pusimos; en local usará appsettings
    var connectionString = builder.Configuration["MongoDbSettings__ConnectionString"] 
                           ?? mongoSettings["ConnectionString"]!;
    
    var mongoUrl = new MongoUrl(connectionString);
    var clientSettings = MongoClientSettings.FromUrl(mongoUrl);

    clientSettings.SslSettings = new SslSettings
    {
        EnabledSslProtocols = SslProtocols.Tls12,
        ServerCertificateValidationCallback = (sender, cert, chain, errors) => true
    };

    return new MongoClient(clientSettings);
});

builder.Services.AddScoped(sp => 
{
    var client = sp.GetRequiredService<IMongoClient>();
    var dbName = builder.Configuration["MongoDbSettings__DatabaseName"] 
                 ?? mongoSettings["DatabaseName"] 
                 ?? "MaratonHub";
    return client.GetDatabase(dbName);
});

// --- 3. REGISTRO DE SERVICIOS (Dependency Injection) ---
builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ITheMovieDBService, TheMovieDBService>();
builder.Services.AddScoped<IMediaCacheRepository, MediaCacheRepository>();
builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

// Worker de Sincronización
builder.Services.AddHostedService<TmdbCacheSyncWorker>();

var app = builder.Build();

// Swagger / OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// IMPORTANTE: UseCors debe ir después de UseRouting (implícito) y antes de MapControllers
app.UseCors("OpenPolicy");

app.UseAuthorization();
app.MapControllers();

app.Run();