namespace HVZ.Persistence.MongoDB.Serializers;

using global::MongoDB.Bson.Serialization;

/// <summary>
///     Offers a convenient way to register all serializers that should be globally registered
///     for their respective type.
/// </summary>
public static class CustomSerializers {
    private static bool Registered  ;

    public static void RegisterAll()
    {
        if (Registered) return;
        Registered = true;
        BsonSerializer.RegisterSerializer(InstantSerializer.Instance);
    }
}