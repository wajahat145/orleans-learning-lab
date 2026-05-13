namespace OrleansChat.Abstractions.Models;

[GenerateSerializer]
public sealed record ChatMessage
{
    [Id(0)] public string MessageId { get; init; } = Guid.NewGuid().ToString("N");
    [Id(1)] public string RoomId { get; init; } = "";
    [Id(2)] public string UserId { get; init; } = "";
    [Id(3)] public string Text { get; init; } = "";
    [Id(4)] public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
}
