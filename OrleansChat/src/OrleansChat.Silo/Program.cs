using System.Net;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using OrleansChat.Abstractions.Streams;
using OrleansChat.Infrastructure.DependencyInjection;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

var host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices((context, services) =>
    {
        services.AddOrleansChatInfrastructure(context.Configuration);
    })
    .UseOrleans((context, siloBuilder) =>
    {
        var mongoConnection = context.Configuration["Mongo:ConnectionString"] ?? "mongodb://localhost:27017";
        var mongoDatabase = context.Configuration["Mongo:DatabaseName"] ?? "orleans_chat_db";

        siloBuilder
            .UseMongoDBClient(mongoConnection)
            .UseMongoDBClustering(options =>
            {
                options.DatabaseName = mongoDatabase;
                options.Strategy = Orleans.Providers.MongoDB.Configuration.MongoDBMembershipStrategy.SingleDocument;
            })
            .UseMongoDBReminders(options => options.DatabaseName = mongoDatabase)
            .AddMongoDBGrainStorage("room-store", options => options.DatabaseName = mongoDatabase)
            .AddMongoDBGrainStorage("user-store", options => options.DatabaseName = mongoDatabase)
            .AddMongoDBGrainStorage("notification-store", options => options.DatabaseName = mongoDatabase)
            .AddMongoDBGrainStorage("presence-store", options => options.DatabaseName = mongoDatabase)
            .AddMemoryStreams(StreamNames.ProviderName)
            .AddMemoryGrainStorage("PubSubStore")
            .Configure<ClusterOptions>(options =>
            {
                options.ClusterId = context.Configuration["Orleans:ClusterId"] ?? "orleans-chat-dev";
                options.ServiceId = context.Configuration["Orleans:ServiceId"] ?? "OrleansChat";
            })
            .Configure<EndpointOptions>(options =>
            {
                options.AdvertisedIPAddress = GetAdvertisedIp();
                options.SiloPort = int.TryParse(Environment.GetEnvironmentVariable("ORLEANS_SILO_PORT"), out var sp) ? sp : 11111;
                options.GatewayPort = int.TryParse(Environment.GetEnvironmentVariable("ORLEANS_GATEWAY_PORT"), out var gp) ? gp : 30000;
            })
            .UseDashboard(options =>
            {
                options.Host = "0.0.0.0";
                options.Port = int.TryParse(Environment.GetEnvironmentVariable("ORLEANS_DASHBOARD_PORT"), out var dp) ? dp : 8080;
            });
    })
    .Build();

await host.RunAsync();

static IPAddress GetAdvertisedIp()
{
    var value = Environment.GetEnvironmentVariable("ORLEANS_ADVERTISED_IP");
    if (!string.IsNullOrWhiteSpace(value) && IPAddress.TryParse(value, out var parsed))
    {
        return parsed;
    }

    var candidates = Dns.GetHostAddresses(Dns.GetHostName());
    var firstV4 = candidates.FirstOrDefault(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(x));
    if (firstV4 is not null)
    {
        return firstV4;
    }

    return IPAddress.Loopback;
}
