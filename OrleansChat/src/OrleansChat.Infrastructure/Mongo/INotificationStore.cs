using OrleansChat.Abstractions.Models;

namespace OrleansChat.Infrastructure.Mongo;

public interface INotificationStore
{
    Task AddAsync(Notification notification, CancellationToken cancellationToken);
    Task<IList<Notification>> GetUnreadAsync(string userId, CancellationToken cancellationToken);
    Task MarkReadAsync(string userId, string notificationId, CancellationToken cancellationToken);
}
