using Newtonsoft.Json;

namespace Dan.Core.Extensions;

public static class DeepCopyExtensions
{
    public static T DeepCopy<T>(this T objectToClone)
    {
        var serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            NullValueHandling = NullValueHandling.Ignore,
        };

        var serializedValue = JsonConvert.SerializeObject(objectToClone, serializerSettings);
        return JsonConvert.DeserializeObject<T>(serializedValue, serializerSettings)!;
    }
}