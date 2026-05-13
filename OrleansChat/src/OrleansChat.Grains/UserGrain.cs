using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;

namespace OrleansChat.Grains;

public sealed class UserGrain : Grain, IUserGrain
{
    private readonly IPersistentState<UserState> _state;

    public UserGrain([PersistentState("user", "user-store")] IPersistentState<UserState> state)
    {
        _state = state;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _state.State.UserId = this.GetPrimaryKeyString();
        _state.State.Profile ??= new UserProfile
        {
            UserId = _state.State.UserId,
            DisplayName = _state.State.UserId
        };

        await _state.WriteStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task ConnectAsync(string connectionId)
    {
        _state.State.ConnectionId = connectionId;
        await _state.WriteStateAsync();

        var presence = GrainFactory.GetGrain<IPresenceGrain>(this.GetPrimaryKeyString());
        try
        {
            await presence.SetOnlineAsync(connectionId);
        }
        catch
        {
        }
    }

    public async Task DisconnectAsync()
    {
        _state.State.ConnectionId = null;
        await _state.WriteStateAsync();

        var presence = GrainFactory.GetGrain<IPresenceGrain>(this.GetPrimaryKeyString());
        try
        {
            await presence.SetOfflineAsync();
        }
        catch
        {
        }
    }

    public async Task JoinRoomAsync(string roomId)
    {
        var room = GrainFactory.GetGrain<IRoomGrain>(roomId);
        await room.JoinAsync(this.GetPrimaryKeyString());

        if (!_state.State.Rooms.Contains(roomId, StringComparer.OrdinalIgnoreCase))
        {
            _state.State.Rooms.Add(roomId);
            await _state.WriteStateAsync();
        }
    }

    public async Task LeaveRoomAsync(string roomId)
    {
        var room = GrainFactory.GetGrain<IRoomGrain>(roomId);
        await room.LeaveAsync(this.GetPrimaryKeyString());

        _state.State.Rooms.RemoveAll(x => x.Equals(roomId, StringComparison.OrdinalIgnoreCase));
        await _state.WriteStateAsync();
    }

    public async Task ReceiveMessageAsync(ChatMessage message)
    {
        var presence = GrainFactory.GetGrain<IPresenceGrain>(this.GetPrimaryKeyString());
        var isOnline = await presence.GetStatusAsync() == PresenceStatus.Online;

        if (!isOnline)
        {
            var notification = GrainFactory.GetGrain<INotificationGrain>(this.GetPrimaryKeyString());
            await notification.SendPushAsync(new PushNotification
            {
                UserId = this.GetPrimaryKeyString(),
                Title = $"New message in {message.RoomId}",
                Body = message.Text
            });
        }
    }

    public Task<UserProfile> GetProfileAsync() => Task.FromResult(_state.State.Profile!);

    [GenerateSerializer]
    public sealed class UserState
    {
        [Id(0)] public string UserId { get; set; } = "";
        [Id(1)] public string? ConnectionId { get; set; }
        [Id(2)] public List<string> Rooms { get; set; } = [];
        [Id(3)] public UserProfile? Profile { get; set; }
    }
}
