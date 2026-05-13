using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;
using OrleansChat.Infrastructure.Mongo;

namespace OrleansChat.Grains;

public sealed class NotificationGrain : Grain, INotificationGrain, IRemindable
{
    private readonly INotificationStore _notificationStore;
    private readonly IPersistentState<NotificationState> _state;

    public NotificationGrain(
        INotificationStore notificationStore,
        [PersistentState("notification", "notification-store")] IPersistentState<NotificationState> state)
    {
        _notificationStore = notificationStore;
        _state = state;
    }

    public Task SendPushAsync(PushNotification notification)
    {
        var item = new Notification
        {
            UserId = this.GetPrimaryKeyString(),
            Type = "push",
            Payload = $"{notification.Title}: {notification.Body}",
            Read = false,
            CreatedUtc = DateTime.UtcNow
        };

        return _notificationStore.AddAsync(item, CancellationToken.None);
    }

    public Task<IList<Notification>> GetUnreadAsync()
    {
        return _notificationStore.GetUnreadAsync(this.GetPrimaryKeyString(), CancellationToken.None);
    }

    public Task MarkReadAsync(string notificationId)
    {
        return _notificationStore.MarkReadAsync(this.GetPrimaryKeyString(), notificationId, CancellationToken.None);
    }

    public async Task ScheduleReminderAsync(string message, TimeSpan delay)
    {
        _state.State.ReminderMessage = message;
        await _state.WriteStateAsync();
        await this.RegisterOrUpdateReminder("notify", delay, TimeSpan.FromMinutes(1));
    }

    public Task ReceiveReminder(string reminderName, TickStatus status)
    {
        var item = new Notification
        {
            UserId = this.GetPrimaryKeyString(),
            Type = "reminder",
            Payload = _state.State.ReminderMessage,
            Read = false,
            CreatedUtc = DateTime.UtcNow
        };

        return _notificationStore.AddAsync(item, CancellationToken.None);
    }

    [GenerateSerializer]
    public sealed class NotificationState
    {
        [Id(0)] public string ReminderMessage { get; set; } = "Reminder";
    }
}
