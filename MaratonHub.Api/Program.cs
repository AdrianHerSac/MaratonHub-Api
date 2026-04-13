using MongoDB.Driver;
using MongoDB.Driver.Core.Configuration;
using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using MaratonHub.Api.Reviews;
using System.Security.Authentication;
using MaratonHub.Api.Reviews.Reposytory;
using MaratonHub.Api.TheMovieDB.Repository;
using MaratonHub.Api.Workers;

var builder = WebApplication.CreateBuilder(args);


// --- CONFIGURACIÓN DE MONGODB ATLAS ---
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings");

builder.Services.AddSingleton<IMongoClient>(sp => 
{
    var connectionString = mongoSettings["ConnectionString"]!;
    var mongoUrl = new MongoUrl(connectionString);
    var clientSettings = MongoClientSettings.FromUrl(mongoUrl);

    // Fix Win32Exception 0x80090304 en Windows:
    clientSettings.AllowInsecureTls = true;
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
    return client.GetDatabase(mongoSettings["DatabaseName"]);
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddScoped<ITheMovieDBService, TheMovieDBService>();
builder.Services.AddScoped<IMediaCacheRepository, MediaCacheRepository>();

builder.Services.AddScoped<IMediaService, MediaService>();
builder.Services.AddScoped<IMediaRepository, MediaRepository>();

builder.Services.AddScoped<IReviewRepository, ReviewRepository>();

builder.Services.AddHostedService<TmdbCacheSyncWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");

app.UseAuthorization();

app.MapControllers();

app.Run();