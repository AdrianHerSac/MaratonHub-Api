using MaratonHub.Api.Notifications.Hubs;
using MaratonHub.Api.Notifications.Models;
using MaratonHub.Api.Notifications.Repositories;
using MaratonHub.Api.Notifications.Services;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace MaratonHub.ApiTest.Notifications;

public class NotificationServiceExtendedTests
{
    private Mock<INotificationRepository> _mockRepo = null!;
    private Mock<IHubContext<NotificationHub>> _mockHubContext = null!;
    private Mock<IHubClients> _mockClients = null!;
    private Mock<IClientProxy> _mockClientProxy = null!;
    private NotificationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepo = new Mock<INotificationRepository>();
        _mockHubContext = new Mock<IHubContext<NotificationHub>>();
        _mockClients = new Mock<IHubClients>();
        _mockClientProxy = new Mock<IClientProxy>();

        _mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(_mockClientProxy.Object);
        _mockHubContext.Setup(h => h.Clients).Returns(_mockClients.Object);

        _service = new NotificationService(_mockRepo.Object, _mockHubContext.Object);
    }

    [Test]
    public async Task CreateAndSendAsync_ShouldSaveNotificationAndSendSignalRMessages()
    {
        // Arrange
        var userId = "user1";
        var title = "Test Title";
        var message = "Test Message";
        var type = "success";
        var groupId = "group1";

        var createdNotification = new Notification
        {
            Id = "notif1",
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            GroupId = groupId,
            Read = false,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepo.Setup(r => r.CreateAsync(It.IsAny<Notification>()))
            .ReturnsAsync(createdNotification);
            
        _mockRepo.Setup(r => r.GetUnreadCountAsync(userId))
            .ReturnsAsync(5);

        // Act
        await _service.CreateAndSendAsync(userId, title, message, type, groupId);

        // Assert
        _mockRepo.Verify(r => r.CreateAsync(It.Is<Notification>(n => 
            n.UserId == userId && 
            n.Title == title && 
            n.Message == message && 
            n.Type == type && 
            n.GroupId == groupId
        )), Times.Once);

        _mockRepo.Verify(r => r.GetUnreadCountAsync(userId), Times.Once);

        _mockClients.Verify(c => c.User(userId), Times.Exactly(2));

        _mockClientProxy.Verify(p => p.SendCoreAsync("ReceiveNotification", It.Is<object[]>(args => args.Length == 1), default), Times.Once);
        _mockClientProxy.Verify(p => p.SendCoreAsync("UnreadCount", It.Is<object[]>(args => args.Length == 1 && (long)args[0] == 5), default), Times.Once);
    }
}

public class NotificationHubTests
{
    [Test]
    public async Task OnConnectedAsync_ShouldExecuteWithoutErrors()
    {
        var hub = new NotificationHub();
        var mockClients = new Mock<IHubCallerClients>();
        var mockContext = new Mock<HubCallerContext>();
        hub.Clients = mockClients.Object;
        hub.Context = mockContext.Object;

        Assert.DoesNotThrowAsync(async () => await hub.OnConnectedAsync());
    }

    [Test]
    public async Task OnDisconnectedAsync_ShouldExecuteWithoutErrors()
    {
        var hub = new NotificationHub();
        var mockClients = new Mock<IHubCallerClients>();
        var mockContext = new Mock<HubCallerContext>();
        hub.Clients = mockClients.Object;
        hub.Context = mockContext.Object;

        Assert.DoesNotThrowAsync(async () => await hub.OnDisconnectedAsync(new Exception("Test error")));
    }
}

public class NotificationRepositoryTests
{
    [Test]
    public async Task CreateAsync_ShouldInsertNotificationAndReturnIt()
    {
        var mockDb = new Mock<MongoDB.Driver.IMongoDatabase>();
        var mockCollection = new Mock<MongoDB.Driver.IMongoCollection<Notification>>();

        mockDb.Setup(db => db.GetCollection<Notification>("notifications", null))
            .Returns(mockCollection.Object);

        var repo = new NotificationRepository(mockDb.Object);
        var notif = new Notification { UserId = "u1", Message = "test" };

        mockCollection.Setup(c => c.InsertOneAsync(notif, null, default))
            .Returns(Task.CompletedTask);

        var result = await repo.CreateAsync(notif);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.UserId, Is.EqualTo("u1"));
        mockCollection.Verify(c => c.InsertOneAsync(notif, null, default), Times.Once);
    }
}
