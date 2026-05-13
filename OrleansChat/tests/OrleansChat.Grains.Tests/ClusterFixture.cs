using Microsoft.Extensions.DependencyInjection;
using Orleans.TestingHost;
using OrleansChat.Abstractions.Models;
using OrleansChat.Abstractions.Streams;
using OrleansChat.Infrastructure.Mongo;

namespace OrleansChat.Grains.Tests;

public sealed class ClusterFixture : IDisposable
{
    public TestCluster Cluster { get; }

    public ClusterFixture()
    {
        var builder = new TestClusterBuilder(1);
        builder.AddSiloBuilderConfigurator<SiloConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    private sealed class SiloConfigurator : ISiloConfigurator
    {
        public void Configure(ISiloBuilder siloBuilder)
        {
            siloBuilder
                .UseInMemoryReminderService()
                .AddMemoryStreams(StreamNames.ProviderName)
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorage("room-store")
                .AddMemoryGrainStorage("user-store")
                .AddMemoryGrainStorage("notification-store")
                .AddMemoryGrainStorage("presence-store");

            siloBuilder.ConfigureServices(services =>
            {
                services.AddSingleton<IMessageStore, InMemoryMessageStore>();
                services.AddSingleton<INotificationStore, InMemoryNotificationStore>();
            });
        }
    }

    private sealed class InMemoryMessageStore : IMessageStore
    {
        private readonly List<ChatMessage> _messages = [];

        public Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
        {
            _messages.Add(message);
            return Task.CompletedTask;
        }

        public Task<IList<ChatMessage>> GetRoomHistoryAsync(string roomId, int count, CancellationToken cancellationToken)
        {
            var output = _messages.Where(x => x.RoomId == roomId).TakeLast(count).ToList();
            return Task.FromResult<IList<ChatMessage>>(output);
        }
    }

    private sealed class InMemoryNotificationStore : INotificationStore
    {
        private readonly List<Notification> _notifications = [];

        public Task AddAsync(Notification notification, CancellationToken cancellationToken)
        {
            _notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task<IList<Notification>> GetUnreadAsync(string userId, CancellationToken cancellationToken)
        {
            var output = _notifications.Where(x => x.UserId == userId && !x.Read).ToList();
            return Task.FromResult<IList<Notification>>(output);
        }

        public Task MarkReadAsync(string userId, string notificationId, CancellationToken cancellationToken)
        {
            var current = _notifications.FirstOrDefault(x => x.UserId == userId && x.NotificationId == notificationId);
            if (current is not null)
            {
                _notifications.Remove(current);
                _notifications.Add(new Notification
                {
                    NotificationId = current.NotificationId,
                    UserId = current.UserId,
                    Type = current.Type,
                    Payload = current.Payload,
                    Read = true,
                    CreatedUtc = current.CreatedUtc
                });
            }

            return Task.CompletedTask;
        }
    }
}
