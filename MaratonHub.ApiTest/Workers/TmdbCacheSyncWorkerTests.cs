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
}
