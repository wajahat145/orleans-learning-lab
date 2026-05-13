using OrleansChat.Abstractions.Models;

namespace OrleansChat.Abstractions.Grains;

public interface INotificationGrain : IGrainWithStringKey
{
    Task ScheduleReminderAsync(string message, TimeSpan delay);
    Task SendPushAsync(PushNotification notification);
    Task<IList<Notification>> GetUnreadAsync();
    Task MarkReadAsync(string notificationId);
}
