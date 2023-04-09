using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using NodaTime;

namespace HVZ.Persistence.MongoDB.Serializers
{
    public class NullableInstantSerializer : SerializerBase<Instant?>
    {
        public static readonly NullableInstantSerializer Instance = new NullableInstantSerializer();

        private NullableInstantSerializer()
        {
        }

        public override Instant? Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {

            BsonType type = context.Reader.GetCurrentBsonType();
            if (type == BsonType.Null)
            {
                context.Reader.ReadNull();
                return null;
            }
            return type switch
            {
                BsonType.DateTime => Instant.FromUnixTimeMilliseconds(context.Reader.ReadDateTime()),
                _ => throw CreateCannotBeDeserializedException()
            };
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, Instant? value)
        {
            if (value is null)
            {
                context.Writer.WriteNull();
                return;
            }

            context.Writer.WriteDateTime(((Instant)value).ToUnixTimeMilliseconds());
        }
    }
}
