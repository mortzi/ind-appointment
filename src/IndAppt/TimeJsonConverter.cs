using System.Text.Json;
using System.Text.Json.Serialization;

namespace IndAppt;

public class TimeJsonConverter : JsonConverter<Time>
{
    public override Time? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var timeParts = reader.GetString()!.Split(":");
        return new Time
        {
            Hour = int.Parse(timeParts[0]), 
            Minute = int.Parse(timeParts[1])
        };
    }

    public override void Write(Utf8JsonWriter writer, Time value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

