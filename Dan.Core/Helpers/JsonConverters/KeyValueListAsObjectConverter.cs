using Newtonsoft.Json;

namespace Dan.Core.Helpers.JsonConverters;

// Solution found here: https://stackoverflow.com/questions/68069271/how-to-serialize-and-deserialize-a-json-object-with-duplicate-property-names-in
public class KeyValueListAsObjectConverter<TValue> : JsonConverter<List<KeyValuePair<string, TValue>>>
{
    public override List<KeyValuePair<string, TValue>> ReadJson(
        JsonReader reader,
        Type objectType,
        List<KeyValuePair<string, TValue>>? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        // We don't need to read this format
        throw new NotImplementedException();
    }

    public override void WriteJson(JsonWriter writer, List<KeyValuePair<string, TValue>>? value, JsonSerializer serializer)
    {
        writer.WriteStartObject();
        foreach (var pair in value ?? [])
        {
            writer.WritePropertyName(pair.Key);
            serializer.Serialize(writer, pair.Value);
        }
        writer.WriteEndObject();
    }
}