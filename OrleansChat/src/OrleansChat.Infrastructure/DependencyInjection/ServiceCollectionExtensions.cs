using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using OrleansChat.Abstractions.Models;
using OrleansChat.Infrastructure.Configuration;
using OrleansChat.Infrastructure.Mongo;

namespace OrleansChat.Infrastructure.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrleansChatInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        TryRegisterMongoConventions();
        TryRegisterMongoClassMaps();

        // Register GuidSerializer to fix "GuidRepresentation is Unspecified" error
        if (!BsonSerializer.LookupSerializer<Guid>().GetType().IsGenericType || 
            BsonSerializer.LookupSerializer<Guid>().GetType().GetGenericTypeDefinition() != typeof(GuidSerializer))
        {
            try 
            {
#pragma warning disable CS0618
                BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
#pragma warning restore CS0618
            }
            catch (BsonSerializationException) { /* Already registered */ }
        }

        services.AddOptions<MongoOptions>().Bind(configuration.GetSection("Mongo"));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoOptions>>().Value;
            return new MongoClient(opts.ConnectionString);
        });

        services.AddSingleton(sp =>
        {
            var opts = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MongoOptions>>().Value;
            var client = sp.GetRequiredService<IMongoClient>();
            return client.GetDatabase(opts.DatabaseName);
        });

        services.AddSingleton<IMessageStore, MongoMessageStore>();
        services.AddSingleton<INotificationStore, MongoNotificationStore>();
        return services;
    }

    private static void TryRegisterMongoConventions()
    {
        try
        {
            var pack = new ConventionPack { new IgnoreExtraElementsConvention(true) };
            ConventionRegistry.Register("orleanschat-conventions", pack, _ => true);
        }
        catch
        {
        }
    }

    private static void TryRegisterMongoClassMaps()
    {
        try
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(ChatMessage)))
            {
                BsonClassMap.RegisterClassMap<ChatMessage>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                    cm.MapMember(x => x.MessageId).SetSerializer(new StringIdSerializer());
                });
            }
        }
        catch
        {
        }

        try
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(Notification)))
            {
                BsonClassMap.RegisterClassMap<Notification>(cm =>
                {
                    cm.AutoMap();
                    cm.SetIgnoreExtraElements(true);
                    cm.MapMember(x => x.NotificationId).SetSerializer(new StringIdSerializer());
                });
            }
        }
        catch
        {
        }
    }
}
