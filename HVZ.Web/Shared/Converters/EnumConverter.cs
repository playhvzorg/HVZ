using System.Text.Json;
using System.Text.Json.Serialization;

namespace HVZ.Web.Server.JsonConverters
{
    public class EnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => (TEnum)Enum.Parse(typeof(TEnum), reader.GetString()!);


        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
    }
}
