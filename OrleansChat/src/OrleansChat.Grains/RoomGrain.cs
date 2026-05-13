using Orleans.Streams;
using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;
using OrleansChat.Abstractions.Streams;
using OrleansChat.Infrastructure.Mongo;

namespace OrleansChat.Grains;

public sealed class RoomGrain : Grain, IRoomGrain
{
    private readonly IPersistentState<RoomState> _state;
    private readonly IMessageStore _messageStore;
    private readonly ILogger<RoomGrain> _logger;
    private IAsyncStream<ChatMessage>? _stream;

    public RoomGrain(
        [PersistentState("room", "room-store")] IPersistentState<RoomState> state,
        IMessageStore messageStore,
        ILogger<RoomGrain> logger)
    {
        _state = state;
        _messageStore = messageStore;
        _logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _stream = this.GetStreamProvider(StreamNames.ProviderName)
            .GetStream<ChatMessage>(StreamId.Create(StreamNames.RoomNamespace, this.GetPrimaryKeyString()));

        _state.State.RoomId = this.GetPrimaryKeyString();
        await _state.WriteStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<string> JoinAsync(string userId)
    {
        if (!_state.State.Members.Contains(userId, StringComparer.OrdinalIgnoreCase))
        {
            _state.State.Members.Add(userId);
            await _state.WriteStateAsync();
        }

        return _state.State.RoomId;
    }

    public async Task LeaveAsync(string userId)
    {
        _state.State.Members.RemoveAll(x => x.Equals(userId, StringComparison.OrdinalIgnoreCase));
        await _state.WriteStateAsync();
    }

    public async Task SendMessageAsync(ChatMessage message)
    {
        var normalized = message with
        {
            RoomId = this.GetPrimaryKeyString(),
            TimestampUtc = DateTime.UtcNow,
            MessageId = string.IsNullOrEmpty(message.MessageId) ? Guid.NewGuid().ToString("N") : message.MessageId
        };

        try
        {
            await _messageStore.AddAsync(normalized, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist chat message for room {RoomId}", normalized.RoomId);
        }
        if (_stream is not null)
        {
            try
            {
                await _stream.OnNextAsync(normalized);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish chat message to stream for room {RoomId}", normalized.RoomId);
            }
        }

        _logger.LogInformation("Room {RoomId} message from {UserId}", normalized.RoomId, normalized.UserId);
    }

    public Task<IList<ChatMessage>> GetHistoryAsync(int count = 50)
    {
        var take = count <= 0 ? 50 : count;
        return _messageStore.GetRoomHistoryAsync(this.GetPrimaryKeyString(), take, CancellationToken.None);
    }

    public Task<IList<string>> GetMembersAsync() => Task.FromResult<IList<string>>(_state.State.Members);

    [GenerateSerializer]
    public sealed class RoomState
    {
        [Id(0)] public string RoomId { get; set; } = "";
        [Id(1)] public List<string> Members { get; set; } = [];
    }
}
