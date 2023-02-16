

using MongoDB.Bson.Serialization;

namespace HVZ.Persistence.MongoDB.Serializers
{
    /// <summary>
    /// Offers a convenient way to register all serializers that should be globally registered
    /// for their respective type.
    /// </summary>
    public static class CustomSerializers
    {
        private static bool Registered = false;

        public static void RegisterAll()
        {
            if (Registered) return;
            Registered = true;
            BsonSerializer.RegisterSerializer(InstantSerializer.Instance);
        }
    }
}