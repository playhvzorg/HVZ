namespace HVZ.Persistence.MongoDB.Serializers;

using global::MongoDB.Bson;
using global::MongoDB.Bson.Serialization;
using global::MongoDB.Bson.Serialization.Serializers;
using NodaTime;

public class InstantSerializer : SerializerBase<Instant> {
    public static readonly InstantSerializer Instance = new();

    private InstantSerializer()
    {
    }

    public override Instant Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
    {
        BsonType type = context.Reader.GetCurrentBsonType();
        return type switch
        {
            BsonType.DateTime => Instant.FromUnixTimeMilliseconds(context.Reader.ReadDateTime()),
            _ => throw CreateCannotBeDeserializedException()
        };
    }

    public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Instant value)
    {
        context.Writer.WriteDateTime(value.ToUnixTimeMilliseconds());
    }
}