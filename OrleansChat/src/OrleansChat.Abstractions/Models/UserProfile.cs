namespace OrleansChat.Abstractions.Models;

[GenerateSerializer]
public sealed class UserProfile
{
    [Id(0)] public string UserId { get; init; } = "";
    [Id(1)] public string DisplayName { get; init; } = "";
    [Id(2)] public string? AvatarUrl { get; init; }
    [Id(3)] public DateTimeOffset CreatedUtc { get; init; } = DateTimeOffset.UtcNow;
}
