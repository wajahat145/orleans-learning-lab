using System.Text.Json;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Http.Json;
using Orleans.Configuration;
using Orleans.Streams;
using OrleansChat.Abstractions.Grains;
using OrleansChat.Abstractions.Models;
using OrleansChat.Abstractions.Streams;
using OrleansChat.Api.Services;
using OrleansChat.Infrastructure.DependencyInjection;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOrleansChatInfrastructure(builder.Configuration);
builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5000, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenAnyIP(5001, o => o.Protocols = HttpProtocols.Http2);
});

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateLogger();
builder.Host.UseSerilog();
builder.Services.AddGrpc();

builder.Host.UseOrleansClient((context, clientBuilder) =>
{
    var mongoConn = context.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
    var mongoDb = context.Configuration["Mongo:DatabaseName"] ?? "orleans_chat_db";

    clientBuilder
        .UseMongoDBClient(mongoConn)
        .UseMongoDBClustering(options =>
        {
            options.DatabaseName = mongoDb;
            options.Strategy = Orleans.Providers.MongoDB.Configuration.MongoDBMembershipStrategy.SingleDocument;
        })
        .AddMemoryStreams(StreamNames.ProviderName)
        .UseConnectionRetryFilter(async (exception, cancellationToken) =>
        {
            Log.Warning("Orleans client failed to connect, retrying in 5s... {Error}", exception.Message);
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            return true;
        })
        .Configure<ClusterOptions>(options =>
        {
            options.ClusterId = context.Configuration["Orleans:ClusterId"] ?? "orleans-chat-dev";
            options.ServiceId = context.Configuration["Orleans:ServiceId"] ?? "OrleansChat";
        });
});

var app = builder.Build();

app.UseCors();

var sseJsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/users/{userId}/connect", async (string userId, ConnectRequest request, IClusterClient client) =>
{
    var user = client.GetGrain<IUserGrain>(userId);
    await user.ConnectAsync(request.ConnectionId ?? Guid.NewGuid().ToString("N"));
    return Results.Ok();
});

app.MapDelete("/api/users/{userId}/connect", async (string userId, IClusterClient client) =>
{
    var user = client.GetGrain<IUserGrain>(userId);
    await user.DisconnectAsync();
    return Results.Ok();
});

app.MapPost("/api/rooms/{roomId}/join", async (string roomId, JoinLeaveRequest request, IClusterClient client) =>
{
    var user = client.GetGrain<IUserGrain>(request.UserId);
    await user.JoinRoomAsync(roomId);
    return Results.Ok();
});

app.MapPost("/api/rooms/{roomId}/leave", async (string roomId, JoinLeaveRequest request, IClusterClient client) =>
{
    var user = client.GetGrain<IUserGrain>(request.UserId);
    await user.LeaveRoomAsync(roomId);
    return Results.Ok();
});

app.MapGet("/api/rooms/{roomId}/members", async (string roomId, IClusterClient client) =>
{
    var room = client.GetGrain<IRoomGrain>(roomId);
    return Results.Ok(await room.GetMembersAsync());
});

app.MapGet("/api/rooms/{roomId}/history", async (string roomId, int? count, IClusterClient client) =>
{
    var room = client.GetGrain<IRoomGrain>(roomId);
    return Results.Ok(await room.GetHistoryAsync(count ?? 50));
});

app.MapPost("/api/rooms/{roomId}/messages", async (string roomId, SendMessageRequest request, IClusterClient client) =>
{
    var room = client.GetGrain<IRoomGrain>(roomId);
    await room.SendMessageAsync(new ChatMessage
    {
        RoomId = roomId,
        UserId = request.UserId,
        Text = request.Text,
        TimestampUtc = DateTime.UtcNow
    });

    return Results.Accepted();
});

app.MapGet("/api/rooms/{roomId}/stream", async (string roomId, HttpContext http, IClusterClient client) =>
{
    http.Response.Headers.Append("Content-Type", "text/event-stream");
    http.Response.Headers.Append("Cache-Control", "no-cache");

    var stream = client.GetStreamProvider(StreamNames.ProviderName)
        .GetStream<ChatMessage>(StreamId.Create(StreamNames.RoomNamespace, roomId));

    var observer = new StreamObserver(async (item, token) =>
    {
        var payload = JsonSerializer.Serialize(item, sseJsonOptions);
        await http.Response.WriteAsync($"data: {payload}\n\n", http.RequestAborted);
        await http.Response.Body.FlushAsync(http.RequestAborted);
    });
    var handle = await stream.SubscribeAsync(observer);

    try
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, http.RequestAborted);
    }
    catch (OperationCanceledException)
    {
    }

    await handle.UnsubscribeAsync();
});

app.MapGet("/api/users/{userId}/notifications", async (string userId, IClusterClient client) =>
{
    var notification = client.GetGrain<INotificationGrain>(userId);
    return Results.Ok(await notification.GetUnreadAsync());
});

app.MapPatch("/api/users/{userId}/notifications", async (string userId, MarkReadRequest request, IClusterClient client) =>
{
    var notification = client.GetGrain<INotificationGrain>(userId);
    await notification.MarkReadAsync(request.NotificationId);
    return Results.Ok();
});

app.MapGrpcService<ChatInternalService>();

try 
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally 
{
    Log.CloseAndFlush();
}

public sealed record ConnectRequest(string? ConnectionId);
public sealed record JoinLeaveRequest(string UserId);
public sealed record SendMessageRequest(string UserId, string Text);
public sealed record MarkReadRequest(string NotificationId);

public sealed class StreamObserver : IAsyncObserver<ChatMessage>
{
    private readonly Func<ChatMessage, StreamSequenceToken?, Task> _onNext;

    public StreamObserver(Func<ChatMessage, StreamSequenceToken?, Task> onNext)
    {
        _onNext = onNext;
    }

    public Task OnCompletedAsync() => Task.CompletedTask;

    public Task OnErrorAsync(Exception ex) => Task.CompletedTask;

    public Task OnNextAsync(ChatMessage item, StreamSequenceToken? token = null)
    {
        return _onNext(item, token);
    }
}
