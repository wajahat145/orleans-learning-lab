namespace OrleansChat.Infrastructure.Configuration;

public sealed class MongoOptions
{
    public string ConnectionString { get; init; } = "mongodb://localhost:27017";
    public string DatabaseName { get; init; } = "orleans_chat_db";
}
