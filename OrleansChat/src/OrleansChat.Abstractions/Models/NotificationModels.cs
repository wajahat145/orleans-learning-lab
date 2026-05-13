namespace OrleansChat.Abstractions.Models;

[GenerateSerializer]
public sealed class Notification
{
    [Id(0)] public string NotificationId { get; init; } = Guid.NewGuid().ToString("N");
    [Id(1)] public string UserId { get; init; } = "";
    [Id(2)] public string Type { get; init; } = "message";
    [Id(3)] public string Payload { get; init; } = "";
    [Id(4)] public bool Read { get; init; }
    [Id(5)] public DateTime CreatedUtc { get; init; } = DateTime.UtcNow;
}

[GenerateSerializer]
public sealed class PushNotification
{
    [Id(0)] public string UserId { get; init; } = "";
    [Id(1)] public string Title { get; init; } = "";
    [Id(2)] public string Body { get; init; } = "";
}
