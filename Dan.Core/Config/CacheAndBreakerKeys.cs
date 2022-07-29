using Dan.Core.Extensions;

namespace Dan.Core.Config
{
    /// <summary>
    /// Cache area/prefix
    /// </summary>
    public enum CacheArea
    {
        /// <summary>
        /// The path as key
        /// </summary>
        Absolute,

        /// <summary>
        /// JWT key
        /// </summary>
        Jwt
    }

    /// <summary>
    /// Breaker area/prefix
    /// </summary>
    public enum BreakerArea
    {
        /// <summary>
        /// The host as key
        /// </summary>
        Absolute,

        /// <summary>
        /// Consent area
        /// </summary>
        Consent,

        /// <summary>
        /// Log area
        /// </summary>
        AltinnLog,

        /// <summary>
        /// ServiceOwner Organization area
        /// </summary>
        AltinnOrganization,

        /// <summary>
        /// Harvest area
        /// </summary>
        Harvest,

        /// <summary>
        /// Evidence area
        /// </summary>
        Evidence,

        /// <summary>
        /// Altinn Reportees area
        /// </summary>
        AltinnReportees
    }

    /// <summary>
    /// Cache and breaker keys
    /// </summary>
    public static class CacheAndBreakerKeys
    {
        /// <summary>
        /// Get the breaker key for a request and area
        /// </summary>
        /// <param name="req">The request</param>
        /// <param name="area">The breaker Area</param>
        /// <returns>The key</returns>
        public static string Key(this HttpRequestMessage req, BreakerArea area)
        {
            if (req.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(req.RequestUri));
            }

            switch (area)
            {
                case BreakerArea.Absolute:
                    return $"Breaker_{req.RequestUri.Host}";
                default:
                    return $"Breaker_{area}_{req.RequestUri.Host}";
            }
        }

        /// <summary>
        /// Get the cache key for a request and area
        /// </summary>
        /// <param name="req">The request</param>
        /// <param name="area">The cache area</param>
        /// <returns>The key</returns>
        public static string Key(this HttpRequestMessage req, CacheArea area)
        {
            if (req.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(req.RequestUri));
            }

            var suffix = req.Content != null ? "_" + req.Content.ReadAsStringAsync().Result.Sha256() : string.Empty;
            switch (area)
            {
                default:
                    return $"Cache_{area}_{req.Method}_{req.RequestUri.AbsoluteUri}{suffix}";
            }
        }
    }
}
