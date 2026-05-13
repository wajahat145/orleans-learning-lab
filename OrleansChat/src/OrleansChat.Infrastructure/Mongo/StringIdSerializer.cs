using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;

namespace OrleansChat.Infrastructure.Mongo;

public sealed class StringIdSerializer : SerializerBase<string>
{
    public override string Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        var bsonType = context.Reader.GetCurrentBsonType();
        return bsonType switch
        {
            BsonType.String => context.Reader.ReadString() ?? "",
            BsonType.Binary => DeserializeBinary(context),
            BsonType.Null => DeserializeNull(context),
            _ => context.Reader.ReadRawBsonDocument().ToString() ?? ""
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, string value)
    {
        context.Writer.WriteString(value);
    }

    private static string DeserializeBinary(BsonDeserializationContext context)
    {
        var data = context.Reader.ReadBinaryData();
        try
        {
#pragma warning disable CS0618
            return data.ToGuid(GuidRepresentation.Standard).ToString("N");
#pragma warning restore CS0618
        }
        catch
        {
            return Convert.ToHexString(data.Bytes);
        }
    }

    private static string DeserializeNull(BsonDeserializationContext context)
    {
        context.Reader.ReadNull();
        return "";
    }
}
