//using HVZ.Persistence.Models;
//using HVZ.Web.Shared.Models;
//using System.Text.Json;
//using System.Text.Json.Serialization;

//namespace HVZ.Web.Shared.Converters
//{


//    public class EventLogInfoConverter : JsonConverter<EventLogInfo>
//    {
//        //public override EventLogInfo? ReadJson(JsonReader reader, Type objectType, EventLogInfo? existingValue, bool hasExistingValue, JsonSerializer serializer)
//        //{
//        //    EventLogInfo? info = (EventLogInfo?)reader.Value;
//        //    if (info == null)
//        //    {
//        //        return null;
//        //    }

//        //    return info switch
//        //    {
//        //        { EventType: GameEvent.GameCreated } => (GameCreatedEventLogInfo?)reader.Value,
//        //        { EventType: GameEvent.Tag } => (TagEventLogInfo?)reader.Value,
//        //        { EventType: GameEvent.PlayerRoleChangedByMod } => (RoleSetEventLogInfo?)reader.Value,
//        //        { EventType: GameEvent.ActiveStatusChanged } => (ActiveStatusChangedEventLogInfo?)reader.Value,
//        //        _ => new GenericEventInfo(info.EventType)
//        //    };
//        //}
//        public override EventLogInfo? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
//        {
//            var info = JsonSerializer.Deserialize<EventLogInfo>(ref reader, options);

//            if (info is null) return null;

//            return info switch
//            {
//                { EventType: GameEvent.GameCreated }
//                    => JsonSerializer.Deserialize<GameCreatedEventLogInfo>(ref reader, options),
//                { EventType: GameEvent.Tag }
//                    => JsonSerializer.Deserialize<TagEventLogInfo>(ref reader, options),
//                { EventType: GameEvent.PlayerRoleChangedByMod }
//                    => JsonSerializer.Deserialize<RoleSetEventLogInfo>(ref reader, options),
//                { EventType: GameEvent.ActiveStatusChanged }
//                    => JsonSerializer.Deserialize<ActiveStatusChangedEventLogInfo>(ref reader, options),
//                _ => new GenericEventInfo(info.EventType)
//            };
//        }

//        public override void Write(Utf8JsonWriter writer, EventLogInfo value, JsonSerializerOptions options)
//        {
//            writer.WriteRawValue(JsonSerializer.Serialize(value, options));
//        }
//    }
//}
