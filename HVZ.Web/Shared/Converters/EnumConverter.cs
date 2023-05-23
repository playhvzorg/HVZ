using Newtonsoft.Json;

namespace HVZ.Web.Server.JsonConverters
{
    public class EnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum ReadJson(
            JsonReader reader,
            Type objectType,
            TEnum existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        ) => (TEnum)Enum.Parse(typeof(TEnum), reader.ReadAsString()!);

        public override void WriteJson(
            JsonWriter writer,
            TEnum value,
            JsonSerializer serializer
        ) => writer.WriteValue(value.ToString());

    }
}
