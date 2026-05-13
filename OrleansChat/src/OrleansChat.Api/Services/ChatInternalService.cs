using Grpc.Core;
using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;

namespace OrleansChat.Api.Services;

public sealed class ChatInternalService : OrleansChat.Api.Grpc.ChatInternal.ChatInternalBase
{
    private readonly IClusterClient _client;

    public ChatInternalService(IClusterClient client)
    {
        _client = client;
    }

    public override async Task<OrleansChat.Api.Grpc.SystemMessageResponse> BroadcastSystemMessage(
        OrleansChat.Api.Grpc.SystemMessageRequest request,
        ServerCallContext context)
    {
        var room = _client.GetGrain<IRoomGrain>(request.RoomId);
        await room.SendMessageAsync(new ChatMessage
        {
            RoomId = request.RoomId,
            UserId = "system",
            Text = request.Text,
            TimestampUtc = DateTime.UtcNow
        });

        return new OrleansChat.Api.Grpc.SystemMessageResponse { Ok = true };
    }

    public override async Task<OrleansChat.Api.Grpc.RoomStatsResponse> GetRoomStats(
        OrleansChat.Api.Grpc.RoomStatsRequest request,
        ServerCallContext context)
    {
        var room = _client.GetGrain<IRoomGrain>(request.RoomId);
        var members = await room.GetMembersAsync();
        return new OrleansChat.Api.Grpc.RoomStatsResponse { RoomId = request.RoomId, MemberCount = members.Count };
    }

    public override async Task StreamPresenceUpdates(
        OrleansChat.Api.Grpc.PresenceRequest request,
        IServerStreamWriter<OrleansChat.Api.Grpc.PresenceUpdate> responseStream,
        ServerCallContext context)
    {
        foreach (var userId in request.UserIds)
        {
            var presence = _client.GetGrain<IPresenceGrain>(userId);
            var status = await presence.GetStatusAsync();
            await responseStream.WriteAsync(new OrleansChat.Api.Grpc.PresenceUpdate
            {
                UserId = userId,
                Status = status.ToString(),
                AtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });
        }
    }
}
