using MongoDB.Driver;
using OrleansChat.Abstractions.Models;

namespace OrleansChat.Infrastructure.Mongo;

public sealed class MongoNotificationStore : INotificationStore
{
    private readonly IMongoCollection<Notification> _notifications;

    public MongoNotificationStore(IMongoDatabase database)
    {
        _notifications = database.GetCollection<Notification>("notifications");
    }

    public Task AddAsync(Notification notification, CancellationToken cancellationToken)
    {
        return _notifications.InsertOneAsync(notification, cancellationToken: cancellationToken);
    }

    public async Task<IList<Notification>> GetUnreadAsync(string userId, CancellationToken cancellationToken)
    {
        var filter = Builders<Notification>.Filter.Where(x => x.UserId == userId && !x.Read);
        return await _notifications.Find(filter)
            .SortByDescending(x => x.CreatedUtc)
            .ToListAsync(cancellationToken);
    }

    public Task MarkReadAsync(string userId, string notificationId, CancellationToken cancellationToken)
    {
        var filter = Builders<Notification>.Filter.Where(x => x.UserId == userId && x.NotificationId == notificationId);
        var update = Builders<Notification>.Update.Set(x => x.Read, true);
        return _notifications.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }
}
