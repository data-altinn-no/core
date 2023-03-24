using System.Globalization;
using System.Reflection;
using Dan.Core.Models;

namespace Dan.Core.Helpers;
public static class VersionHelper
{
    private static VersionInfo? _versionInfo;

    public static VersionInfo GetVersionInfo()
    {
        if (_versionInfo != null) return _versionInfo;

        var assembly = Assembly.GetExecutingAssembly();
        _versionInfo = new VersionInfo
        {
            Name = assembly.GetName().Name ?? "dancore",
            Built = GetBuildDate(assembly).ToString(@"yyyy-MM-dd\THH:mm:sszzz"),
            Commit = ThisAssembly.Git.Commit,
            CommitDate = ThisAssembly.Git.CommitDate
        };

        return _versionInfo;
    }

    private static DateTime GetBuildDate(Assembly assembly)
    {
        const string buildVersionMetadataPrefix = "+build";

        var attribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (attribute?.InformationalVersion != null)
        {
            var value = attribute.InformationalVersion;
            var index = value.IndexOf(buildVersionMetadataPrefix, StringComparison.Ordinal);
            if (index > 0)
            {
                value = value[(index + buildVersionMetadataPrefix.Length)..];
                if (DateTime.TryParseExact(value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
                {
                    return result;
                }
            }
        }

        return default;
    }
}
