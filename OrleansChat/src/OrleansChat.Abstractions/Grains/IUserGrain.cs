using OrleansChat.Abstractions.Models;

namespace OrleansChat.Abstractions.Grains;

public interface IUserGrain : IGrainWithStringKey
{
    Task ConnectAsync(string connectionId);
    Task DisconnectAsync();
    Task JoinRoomAsync(string roomId);
    Task LeaveRoomAsync(string roomId);
    Task ReceiveMessageAsync(ChatMessage message);
    Task<UserProfile> GetProfileAsync();
}
