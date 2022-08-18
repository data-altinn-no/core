using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Dan.Core.UnitTest.Helpers
{
    /// <summary>
    /// Helper methods for unit test assertions
    /// </summary>
    public static class AssertHelper
    {
        /// <summary>
        /// Assert that the content of all objects have the same type and contents
        /// </summary>
        /// <param name="objects">Array of objects to compare</param>
        public static void AreSameContent(params object[] objects)
        {
            var allAreTheSame = objects.Select(x => SerializedSha256(x)).Distinct().Count() == 1;
            if (!allAreTheSame)
            {
                var summary = objects.Select(x => $"{x.GetType()}:\"{x.ToString()}\" = {SerializedSha256(x)}");
                var summaryText = string.Join("\n", summary);
                throw new NotSameContentsException($"All objects are not the same.\n{summaryText}");
            }
        }

        private static string SerializedSha256(object value)
        {
            var serializedValue = JsonConvert.SerializeObject(value, new JsonSerializerSettings()
            {
                TypeNameHandling = TypeNameHandling.Auto,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            });

            using (SHA256 hash = SHA256.Create())
            {
                return String.Concat(hash
                  .ComputeHash(Encoding.UTF8.GetBytes(serializedValue))
                  .Select(item => item.ToString("x2")));
            }
        }
    }

    /// <summary>
    /// Not same contents exception
    /// </summary>
    public class NotSameContentsException : Exception
    {
        /// <summary>
        /// Not same contents exception
        /// </summary>
        public NotSameContentsException()
        {
        }

        /// <summary>
        /// Not same contents exception
        /// </summary>
        /// <param name="message">Exception Message</param>
        public NotSameContentsException(string message) : base(message)
        {
        }

        /// <summary>
        /// Not same contents exception
        /// </summary>
        /// <param name="message">Exception Message</param>
        /// <param name="innerException">Inner exception</param>
        public NotSameContentsException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Not same contents exception
        /// </summary>
        /// <param name="info">Serialization Info</param>
        /// <param name="context">Streaming context</param>
        protected NotSameContentsException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
