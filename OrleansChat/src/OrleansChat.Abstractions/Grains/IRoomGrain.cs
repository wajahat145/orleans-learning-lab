using OrleansChat.Abstractions.Models;

namespace OrleansChat.Abstractions.Grains;

public interface IRoomGrain : IGrainWithStringKey
{
    Task<string> JoinAsync(string userId);
    Task LeaveAsync(string userId);
    Task SendMessageAsync(ChatMessage message);
    Task<IList<ChatMessage>> GetHistoryAsync(int count = 50);
    Task<IList<string>> GetMembersAsync();
}
