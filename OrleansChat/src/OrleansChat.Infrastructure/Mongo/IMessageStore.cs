using OrleansChat.Abstractions.Models;

namespace OrleansChat.Infrastructure.Mongo;

public interface IMessageStore
{
    Task AddAsync(ChatMessage message, CancellationToken cancellationToken);
    Task<IList<ChatMessage>> GetRoomHistoryAsync(string roomId, int count, CancellationToken cancellationToken);
}
