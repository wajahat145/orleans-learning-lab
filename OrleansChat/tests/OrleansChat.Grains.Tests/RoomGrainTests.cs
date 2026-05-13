using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;

namespace OrleansChat.Grains.Tests;

public sealed class RoomGrainTests : IClassFixture<ClusterFixture>
{
    private readonly ClusterFixture _fixture;

    public RoomGrainTests(ClusterFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Join_Send_And_GetHistory_Works()
    {
        var user = _fixture.Cluster.GrainFactory.GetGrain<IUserGrain>("user-a");
        await user.ConnectAsync("conn-1");
        await user.JoinRoomAsync("general");

        var room = _fixture.Cluster.GrainFactory.GetGrain<IRoomGrain>("general");
        await room.SendMessageAsync(new ChatMessage { RoomId = "general", UserId = "user-a", Text = "hello" });

        var members = await room.GetMembersAsync();
        var history = await room.GetHistoryAsync(10);

        Assert.Contains("user-a", members);
        Assert.Single(history);
        Assert.Equal("hello", history[0].Text);
    }
}
