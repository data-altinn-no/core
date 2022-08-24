using Dan.Common.Attributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Dan.Core.Middleware;

/// <summary>
/// JSON contract resolver that filters out properties with a Hidden attribute. Used for removing properties given over HTTP to the user that are not relevant.
/// </summary>
public class HiddenPropertyContractResolver : DefaultContractResolver
{
    /// <inheritdoc />
    protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
    {
        IList<JsonProperty> properties = base.CreateProperties(type, memberSerialization);
        IList<string> propertiesWithHiddenAttribute = type.GetProperties().Where(
            x => x.GetCustomAttributes(true).Any(
                y => y is HiddenAttribute)).Select(x => x.Name).ToList();

        return properties.Where(p => !propertiesWithHiddenAttribute.Contains(p.UnderlyingName!)).ToList();
    }
}