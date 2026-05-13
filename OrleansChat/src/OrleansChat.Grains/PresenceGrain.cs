using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;

namespace OrleansChat.Grains;

public sealed class PresenceGrain : Grain, IPresenceGrain
{
    private readonly IPersistentState<PresenceState> _state;

    public PresenceGrain([PersistentState("presence", "presence-store")] IPersistentState<PresenceState> state)
    {
        _state = state;
    }

    public async Task SetOnlineAsync(string connectionId)
    {
        _state.State.ConnectionId = connectionId;
        _state.State.Status = PresenceStatus.Online;
        _state.State.LastSeenUtc = DateTimeOffset.UtcNow;
        await _state.WriteStateAsync();
    }

    public async Task SetOfflineAsync()
    {
        _state.State.Status = PresenceStatus.Offline;
        _state.State.ConnectionId = null;
        _state.State.LastSeenUtc = DateTimeOffset.UtcNow;
        await _state.WriteStateAsync();
    }

    public Task<PresenceStatus> GetStatusAsync() => Task.FromResult(_state.State.Status);

    public Task<DateTimeOffset> GetLastSeenAsync() => Task.FromResult(_state.State.LastSeenUtc);

    [GenerateSerializer]
    public sealed class PresenceState
    {
        [Id(0)] public string? ConnectionId { get; set; }
        [Id(1)] public PresenceStatus Status { get; set; }
        [Id(2)] public DateTimeOffset LastSeenUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
