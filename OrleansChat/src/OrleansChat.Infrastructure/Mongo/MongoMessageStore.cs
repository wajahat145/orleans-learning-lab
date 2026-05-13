using MongoDB.Driver;
using OrleansChat.Abstractions.Models;

namespace OrleansChat.Infrastructure.Mongo;

public sealed class MongoMessageStore : IMessageStore
{
    private readonly IMongoCollection<ChatMessage> _messages;

    public MongoMessageStore(IMongoDatabase database)
    {
        _messages = database.GetCollection<ChatMessage>("messages");
    }

    public Task AddAsync(ChatMessage message, CancellationToken cancellationToken)
    {
        return _messages.InsertOneAsync(message, cancellationToken: cancellationToken);
    }

    public async Task<IList<ChatMessage>> GetRoomHistoryAsync(string roomId, int count, CancellationToken cancellationToken)
    {
        var filter = Builders<ChatMessage>.Filter.Eq(x => x.RoomId, roomId);
        var list = await _messages.Find(filter)
            .SortByDescending(x => x.TimestampUtc)
            .Limit(count)
            .ToListAsync(cancellationToken);

        return list;
    }
}
