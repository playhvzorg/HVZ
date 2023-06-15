//using Newtonsoft.Json;
using NodaTime;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HVZ.Web.Server.JsonConverters
{
    public class InstantConverter : JsonConverter<Instant>
    {
        public override Instant Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Instant.FromUnixTimeMilliseconds(reader.GetInt64());

        public override void Write(Utf8JsonWriter writer, Instant value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.ToUnixTimeMilliseconds());
    }
}
