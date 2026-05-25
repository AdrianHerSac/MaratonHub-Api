using MaratonHub.Api.Workers;
using MaratonHub.Api.TheMovieDB.Services;
using MaratonHub.Api.UserMedia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace MaratonHub.ApiTest.Workers;

public class TmdbCacheSyncWorkerTests
{
    [Test]
    public void TmdbCacheSyncWorker_ShouldExist()
    {
        var type = typeof(TmdbCacheSyncWorker);
        Assert.That(type, Is.Not.Null);
    }

    [Test]
    public void TmdbCacheSyncWorker_ShouldExtendBackgroundService()
    {
        Assert.That(typeof(TmdbCacheSyncWorker).BaseType!.Name, Is.EqualTo("BackgroundService"));
    }

    [Test]
    public void TmdbCacheSyncWorker_ShouldHaveCorrectConstructor()
    {
        var ctors = typeof(TmdbCacheSyncWorker).GetConstructors();
        Assert.That(ctors.Length, Is.EqualTo(1));
        var p = ctors[0].GetParameters();
        Assert.That(p.Length, Is.EqualTo(2));
        Assert.That(p[0].ParameterType, Is.EqualTo(typeof(IServiceScopeFactory)));
        Assert.That(p[1].ParameterType.Name, Does.Contain("ILogger"));
    }

    [Test]
    public void TmdbCacheSyncWorker_ShouldOverrideExecuteAsync()
    {
        var method = typeof(TmdbCacheSyncWorker).GetMethod("ExecuteAsync",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.ReturnType, Is.EqualTo(typeof(Task)));
    }

    [Test]
    public void TmdbCacheSyncWorker_CanBeInstantiated()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockLogger = new Mock<ILogger<TmdbCacheSyncWorker>>();

        var worker = new TmdbCacheSyncWorker(mockScopeFactory.Object, mockLogger.Object);

        Assert.That(worker, Is.Not.Null);
    }

    [Test]
    public async Task TmdbCacheSyncWorker_ShouldRespectCancellation()
    {
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockLogger = new Mock<ILogger<TmdbCacheSyncWorker>>();
        var worker = new TmdbCacheSyncWorker(mockScopeFactory.Object, mockLogger.Object);

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // StartAsync should complete without hanging when cancelled
        await worker.StartAsync(cts.Token);
        await Task.Delay(100); // Give it a moment
        await worker.StopAsync(CancellationToken.None);

        Assert.Pass("Worker respected cancellation token and stopped.");
    }
    [Test]
    public async Task TmdbCacheSyncWorker_RunSyncAsync_SyncsMoviesAndTvShows()
    {
        // Setup Mocks
        var mockScopeFactory = new Mock<IServiceScopeFactory>();
        var mockScope = new Mock<IServiceScope>();
        var mockServiceProvider = new Mock<IServiceProvider>();
        var mockTmdbService = new Mock<ITheMovieDBService>();
        var mockMediaRepo = new Mock<IMediaRepository>();
        var mockLogger = new Mock<ILogger<TmdbCacheSyncWorker>>();

        mockScopeFactory.Setup(s => s.CreateScope()).Returns(mockScope.Object);
        // IServiceScopeFactory in .NET 6+ has CreateAsyncScope() extension which calls CreateScope() under the hood, 
        // or we might need to mock IAsyncServiceProvider if it casts it. Let's just mock GetService on ServiceProvider.
        mockScope.Setup(s => s.ServiceProvider).Returns(mockServiceProvider.Object);
        
        mockServiceProvider.Setup(p => p.GetService(typeof(ITheMovieDBService))).Returns(mockTmdbService.Object);
        mockServiceProvider.Setup(p => p.GetService(typeof(IMediaRepository))).Returns(mockMediaRepo.Object);

        // Setup TMDB Service
        mockTmdbService.Setup(t => t.GetChangedMovieIdsAsync()).ReturnsAsync(new List<int> { 1, 2 });
        mockTmdbService.Setup(t => t.GetChangedTvShowIdsAsync()).ReturnsAsync(new List<int> { 10, 20 });
        
        mockTmdbService.Setup(t => t.GetMovieDetailsAsync(1)).ReturnsAsync(new MaratonHub.Api.TheMovieDB.Dtos.MovieDto { Id = 1, Title = "Movie 1" });
        mockTmdbService.Setup(t => t.GetMovieDetailsAsync(2)).ReturnsAsync((MaratonHub.Api.TheMovieDB.Dtos.MovieDto?)null);
        
        mockTmdbService.Setup(t => t.GetTvShowDetailsAsync(10)).ReturnsAsync(new MaratonHub.Api.TheMovieDB.Dtos.TvShowDto { Id = 10, Name = "TV 10" });
        mockTmdbService.Setup(t => t.GetTvShowDetailsAsync(20)).ThrowsAsync(new Exception("Network error"));

        // Setup Media Repo
        mockMediaRepo.Setup(m => m.ExistsAsync(1, "movie")).ReturnsAsync(true);
        mockMediaRepo.Setup(m => m.ExistsAsync(2, "movie")).ReturnsAsync(true); // TMDB will return null
        mockMediaRepo.Setup(m => m.ExistsAsync(10, "tv")).ReturnsAsync(true);
        mockMediaRepo.Setup(m => m.ExistsAsync(20, "tv")).ReturnsAsync(true); // TMDB will throw

        mockMediaRepo.Setup(m => m.UpsertByTmdbIdAsync(It.IsAny<Media>())).Returns(Task.CompletedTask);

        var worker = new TmdbCacheSyncWorker(mockScopeFactory.Object, mockLogger.Object);

        // Invoke private RunSyncAsync via reflection
        var method = typeof(TmdbCacheSyncWorker).GetMethod("RunSyncAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        await (Task)method!.Invoke(worker, new object[] { CancellationToken.None })!;

        // Verify
        mockMediaRepo.Verify(m => m.UpsertByTmdbIdAsync(It.Is<Media>(x => x.ExternalApiId == 1 && x.MediaType == "movie")), Times.Once);
        mockMediaRepo.Verify(m => m.UpsertByTmdbIdAsync(It.Is<Media>(x => x.ExternalApiId == 10 && x.MediaType == "tv")), Times.Once);
        
        // Movie 2 returned null from TMDB, should not be upserted
        mockMediaRepo.Verify(m => m.UpsertByTmdbIdAsync(It.Is<Media>(x => x.ExternalApiId == 2)), Times.Never);
        // TV 20 threw exception from TMDB, should not be upserted
        mockMediaRepo.Verify(m => m.UpsertByTmdbIdAsync(It.Is<Media>(x => x.ExternalApiId == 20)), Times.Never);
    }
}
