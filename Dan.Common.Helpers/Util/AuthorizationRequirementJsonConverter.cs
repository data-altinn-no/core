namespace Dan.Common.Helpers.Util;

/// <summary>
/// This converter adds support for serializing authorization requirements from plugins that use System.Text.Json,
/// adding Newtonsoft.Json-compatible type annotations allow for correct deserialization in Core of authorization requirements.
///
/// TODO! Core should migrate to System.Text.Json as well. This will require the Read-method to be implemented
/// See https://stackoverflow.com/a/61207456
/// </summary>
public class AuthorizationRequirementJsonConverter : JsonConverter<Requirement>
{
    public override Requirement Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, Requirement value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        var valueType = value.GetType();
        var valueAssemblyName = valueType.Assembly.GetName();
        writer.WriteString("$type", $"{valueType.FullName}, {valueAssemblyName.Name}");

        var json = JsonSerializer.Serialize(value, value.GetType(), options);
        using (var document = JsonDocument.Parse(json, new JsonDocumentOptions
        {
            AllowTrailingCommas = options.AllowTrailingCommas,
            MaxDepth = options.MaxDepth
        }))
        {
            foreach (var jsonProperty in document.RootElement.EnumerateObject())
                jsonProperty.WriteTo(writer);
        }

        writer.WriteEndObject();
    }
}