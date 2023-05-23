using Newtonsoft.Json;
using NodaTime;

namespace HVZ.Web.Server.JsonConverters
{
    public class InstantConverter : JsonConverter<Instant>
    {
        public override Instant ReadJson(
            JsonReader reader,
            Type objectType,
            Instant existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        ) => Instant.FromDateTimeUtc(reader.ReadAsDateTime()!.Value);


        public override void WriteJson(
            JsonWriter writer,
            Instant value,
            JsonSerializer serializer
        ) => writer.WriteValue(new DateTime(value.ToUnixTimeTicks()));
    }
}
