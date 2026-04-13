using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using MaratonHub.Api.UserMedia.Models;

namespace MaratonHub.Api.Workers;

/// <summary>
/// BackgroundService que se ejecuta cada 24 horas para sincronizar los datos
/// almacenados en MongoDB Atlas con los cambios diarios reportados por TMDB.
/// Solo actualiza los items que ya existen en la colección "media".
/// </summary>
public class TmdbCacheSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TmdbCacheSyncWorker> _logger;

    // Cambia a TimeSpan.FromMinutes(1) para probar sin esperar 24h
    private static readonly TimeSpan SyncInterval = TimeSpan.FromDays(1);

    public TmdbCacheSyncWorker(IServiceScopeFactory scopeFactory, ILogger<TmdbCacheSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[TmdbCacheSyncWorker] Worker registered. First sync will run in {Interval}.", SyncInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            // Espera el intervalo antes de cada ciclo de sincronización
            await Task.Delay(SyncInterval, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            _logger.LogInformation("[TmdbCacheSyncWorker] Starting daily TMDB sync at {Time} UTC.", DateTime.UtcNow);
            await RunSyncAsync(stoppingToken);
        }

        _logger.LogInformation("[TmdbCacheSyncWorker] Worker stopping.");
    }

    private async Task RunSyncAsync(CancellationToken stoppingToken)
    {
        int moviesUpdated = 0, tvUpdated = 0;

        // Creamos un scope para resolver las dependencias Scoped
        await using var scope = _scopeFactory.CreateAsyncScope();
        var tmdbService = scope.ServiceProvider.GetRequiredService<ITheMovieDBService>();
        var mediaRepo = scope.ServiceProvider.GetRequiredService<IMediaRepository>();

        // ── Películas ────────────────────────────────────────────────────────
        List<int> changedMovieIds;
        try
        {
            changedMovieIds = await tmdbService.GetChangedMovieIdsAsync();
            _logger.LogInformation("[TmdbCacheSyncWorker] {Count} changed movie IDs received from TMDB.", changedMovieIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TmdbCacheSyncWorker] Failed to fetch changed movie IDs. Aborting sync.");
            return;
        }

        foreach (var movieId in changedMovieIds)
        {
            if (stoppingToken.IsCancellationRequested) break;
            try
            {
                var exists = await mediaRepo.ExistsAsync(movieId, "movie");
                if (!exists) continue;

                var dto = await tmdbService.GetMovieDetailsAsync(movieId);
                if (dto == null) continue;

                var media = new Media
                {
                    ExternalApiId = dto.Id,
                    Title = dto.Title,
                    MediaType = "movie",
                    PosterPath = dto.PosterPath ?? string.Empty
                };

                await mediaRepo.UpsertByTmdbIdAsync(media);
                moviesUpdated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TmdbCacheSyncWorker] Error processing movie ID {MovieId}. Skipping.", movieId);
            }
        }

        // ── Series de TV ──────────────────────────────────────────────────────
        List<int> changedTvIds;
        try
        {
            changedTvIds = await tmdbService.GetChangedTvShowIdsAsync();
            _logger.LogInformation("[TmdbCacheSyncWorker] {Count} changed TV show IDs received from TMDB.", changedTvIds.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TmdbCacheSyncWorker] Failed to fetch changed TV show IDs.");
            changedTvIds = new List<int>();
        }

        foreach (var tvId in changedTvIds)
        {
            if (stoppingToken.IsCancellationRequested) break;
            try
            {
                var exists = await mediaRepo.ExistsAsync(tvId, "tv");
                if (!exists) continue;

                var dto = await tmdbService.GetTvShowDetailsAsync(tvId);
                if (dto == null) continue;

                var media = new Media
                {
                    ExternalApiId = dto.Id,
                    Title = dto.Name,
                    MediaType = "tv",
                    PosterPath = dto.PosterPath ?? string.Empty
                };

                await mediaRepo.UpsertByTmdbIdAsync(media);
                tvUpdated++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[TmdbCacheSyncWorker] Error processing TV show ID {TvId}. Skipping.", tvId);
            }
        }

        _logger.LogInformation(
            "[TmdbCacheSyncWorker] Sync complete. Movies updated: {Movies} | TV shows updated: {Tv}.",
            moviesUpdated, tvUpdated);
    }
}
