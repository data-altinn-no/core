/*using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using Nadobe.Core.Services;
using Nadobe.Common.Interfaces;
using Nadobe.Common;
using Nadobe.Common.Util;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;

namespace Nadobe.Core.UnitTest
{
    [TestClass]
    [ExcludeFromCodeCoverage]
    public class TokenRequesterTest
    {
        private ILogger tracer = Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new Mock<IHttpClientFactory>();

        [TestInitialize]
        public void Initialize()
        {
            _mockHttpClientFactory.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(TestHelpers.GetHttpClientMock("[{}]"));
        }

        private ICache EmptyCache {
            get {
                var cache = new UnitTestCache(Guid.NewGuid().ToString());
                cache.Clear();
                return cache;
            }
        }

        [TestMethod]
        public void TokenRequester_Auth()
        {
            var cache = EmptyCache;
            var jwt = Guid.NewGuid().ToString();
            var masterkey = Guid.NewGuid().ToString();

            var requester = new TokenRequesterService(MakeFakeClient(cache, jwt, masterkey, (x, n) => true), cache, masterkey);
            var result = requester.GetToken().Result;
            Assert.AreEqual(jwt, result);
        }

        [TestMethod]
        public void TokenRequester_FailAuth()
        {
            var cache = EmptyCache;
            var jwt = Guid.NewGuid().ToString();
            var masterkey = Guid.NewGuid().ToString();

            var requester = new TokenRequester(MakeFakeClient(cache, jwt, masterkey, (x, n) => true), cache, "INVALID_KEY");
            var result = requester.GetToken().Result;
            Assert.AreEqual(null, result);
        }

        [TestMethod]
        public void TokenRequester_HitCache()
        {
            var cache = EmptyCache;
            var jwt = Guid.NewGuid().ToString();
            var masterkey = Guid.NewGuid().ToString();

            var requester = new TokenRequester(MakeFakeClient(cache, jwt, masterkey, (x, n) =>
            {
                return n <= 1;
            }), cache, masterkey)
            {
                EarlyTokenExpiration = TimeSpan.FromMilliseconds(0)
            };

            var result1 = requester.GetToken().Result;
            var result2 = requester.GetToken().Result;

            Assert.AreEqual(jwt, result1);
            Assert.AreEqual(result1, result2);
        }

        [TestMethod]
        [Ignore]
        [ExpectedException(typeof(HttpRequestException))]
        public void TokenRequester_CacheExpire()
        {
            var cache = EmptyCache;
            var jwt = Guid.NewGuid().ToString();
            var masterkey = Guid.NewGuid().ToString();

            var requester = new TokenRequester(MakeFakeClient(cache, jwt, masterkey, (x, n) =>
            {
                return n <= 1;
            }), cache, masterkey)
            {
                EarlyTokenExpiration = TimeSpan.FromMilliseconds(0)
            };

            var result1 = requester.GetToken().Result;

            Thread.Sleep(300);

            UnAggregateExceptions(() => {
                var result = requester.GetToken().Result;
            });
        }

        [TestMethod]
        [Ignore]
        // [ExpectedException(typeof(CircuitBreakerOpenException))]
        public void TokenRequester_HitBreaker()
        {
            var cache = EmptyCache;
            var jwt = Guid.NewGuid().ToString();
            var masterkey = Guid.NewGuid().ToString();

            var requester = new TokenRequester(MakeFakeClient(cache, jwt, masterkey, (x, n) => {
                return n > 1;
            }), cache, masterkey);
            
            try
            {
                var result = requester.GetToken().Result;
            }
            catch
            {
            }

            UnAggregateExceptions(() => { 
                var result = requester.GetToken().Result;
            });
        }

        private string[] validHosts = new string[] { "localhost" };

        private HttpClient MakeFakeClient(ICache cache, string jwt, string masterkey, Func<HttpRequestMessage, int, bool> successFunction)
        {
            var hits = 0;
            var server = new Func<HttpRequestMessage, HttpResponseMessage>(req =>
            {
                hits++;

                if (successFunction != null && !successFunction.Invoke(req, hits))
                {
                    return null;
                }

                if (req.Method != HttpMethod.Post)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound)
                    {
                        Content = new StringContent("Invalid http method for action"),
                    };
                }

                bool requestJson = req.GetHeader("Accept").Contains("application/json");
                string contents = req.Content?.ReadAsStringAsync().Result;
                if (validHosts.Contains(req.RequestUri.Host) && requestJson && contents != null)
                {
                    var contentType = req.Content.GetHeader("Content-Type");
                    if (!contentType.Contains("application/json"))
                    {
                        return new HttpResponseMessage(HttpStatusCode.BadRequest)
                        {
                            Content = new StringContent("Invalid content type")
                        };
                    }

                    var request = JsonConvert.DeserializeObject<TokenRequestModel>(contents);
                    if (request.Secret == masterkey)
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(new TokenResultModel { Success = true, Expire = DateTime.Now.AddMilliseconds(250), JWT = jwt })),
                        };
                    }
                    else
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = new StringContent(JsonConvert.SerializeObject(new TokenResultModel { Success = false })),
                        };
                    }
                }

                return null;
            });

            return TestHelpers.GetHttpClientMock(server);
        }

        // Method to flatten exceptions from tasks where you whish to handle them directly (f.ex. ExpectedException in an unit test using async methods)
        private void UnAggregateExceptions(Action action)
        {
            try
            {
                action();
            }
            catch (AggregateException ae)
            {
                foreach (var ex in ae.InnerExceptions)
                {
                    throw ex;
                }
            }
        }
    }
}
*/