using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;

namespace Dan.Core.Helpers;
public class ValidJsonTypesSerializationBinder : ISerializationBinder
{
    // To maintain backwards compatibility with serialized data before using an ISerializationBinder.
    private static readonly DefaultSerializationBinder Binder = new();

    public Type BindToType(string? assemblyName, string typeName)
    {
        // Compatibility with legacy V3
        typeName = typeName.Replace("Nadobe", "Dan");
        if (assemblyName is "Common")
        {
            assemblyName = "Dan.Common";
        }

        if (assemblyName != null && !assemblyName.StartsWith("Dan"))
        {
            throw new InvalidDataException($"Unable to deserialize from assembly \"{assemblyName}\" - only types within Dan assemblies are allowed");
        }

        if (!typeName.StartsWith("Dan"))
        {
            throw new InvalidDataException($"Unable to deserialize to type \"{typeName}\" - only types within Dan namespaces are allowed");
        }

        return Binder.BindToType(assemblyName, typeName);
    }

    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        Binder.BindToName(serializedType, out assemblyName, out typeName);
    }
}
