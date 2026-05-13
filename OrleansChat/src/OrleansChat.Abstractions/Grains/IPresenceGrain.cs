using OrleansChat.Abstractions.Models;

namespace OrleansChat.Abstractions.Grains;

public interface IPresenceGrain : IGrainWithStringKey
{
    Task SetOnlineAsync(string connectionId);
    Task SetOfflineAsync();
    Task<PresenceStatus> GetStatusAsync();
    Task<DateTimeOffset> GetLastSeenAsync();
}
