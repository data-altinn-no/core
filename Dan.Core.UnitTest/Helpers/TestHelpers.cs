using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Dan.Common.Models;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace Dan.Core.UnitTest.Helpers
{
    /// <summary>
    /// Test helpers for all unit and integration test projects in NADOBE
    /// </summary>
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a mock HttpRequest GET message that can be passed to HTTP triggered functions
        /// </summary>
        /// <param name="hostName">
        /// The host name.
        /// </param>
        /// <param name="queryString">
        /// The query string.
        /// </param>
        /// <returns>
        /// A <see cref="HttpRequestMessage"/>.
        /// </returns>
        public static HttpRequestMessage CreateGetRequest(string hostName = null, Dictionary<string, string> queryString = null)
        {
            var requestPath = string.IsNullOrWhiteSpace(hostName) ? "https://localhost" : hostName;
            requestPath += queryString == null ? string.Empty : $"?{string.Join("&", queryString.Select(kvp => $"{kvp.Key}={kvp.Value}"))}";
            var request = new HttpRequestMessage
                              {
                                  Method = HttpMethod.Get,
                                  RequestUri = new Uri(requestPath)
                              };

            // request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
            return request;
        }

        /// <summary>
        /// Creates a mock HttpRequest GET message that can be passed to HTTP triggered functions
        /// </summary>
        /// <param name="obj">
        /// The object which will be passed as the JSON request body.
        /// </param>
        /// <param name="hostName">
        /// The host name.
        /// </param>
        /// <typeparam name="T">
        /// The type of the request body
        /// </typeparam>
        /// <returns>
        /// The <see cref="HttpRequestMessage"/>.
        /// </returns>
        public static HttpRequestMessage CreatePostRequest<T>(T obj, string hostName = null)
        {
            var requestPath = string.IsNullOrWhiteSpace(hostName) ? "https://localhost" : hostName;
            var json = JsonConvert.SerializeObject(obj);
            var request = new HttpRequestMessage
                              {
                                  Method = HttpMethod.Post,
                                  RequestUri = new Uri(requestPath),
                                  Content = new StringContent(json, Encoding.UTF8, "application/json")
                              };

            // request.Properties.Add(HttpPropertyKeys.HttpConfigurationKey, new HttpConfiguration());
            return request;
        }

        /// <summary>
        /// Gets a mocked HTTP client with an optional response body
        /// </summary>
        /// <param name="responseBody">
        /// The response body.
        /// </param>
        /// <returns>
        /// A <see cref="HttpClient"/>.
        /// </returns>
        /// <exception cref="Exception">
        /// Throws if non-GET requests are made
        /// </exception>
        public static HttpClient GetSafeHttpClientMock(string responseBody = "")
        {
            var handler = new Mock<HttpClientHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task<HttpResponseMessage>.Factory.StartNew(() =>
                    {
                        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody) };
                    })).Callback<HttpRequestMessage, CancellationToken>(
                    (r, _) =>
                        {
                        if (r.Method != HttpMethod.Get)
                        {
                            throw new Exception("GetSafeHttpClientMock() expected GET");
                        }
                        });

            return new HttpClient(handler.Object);
        }

        /// <summary>
        /// Gets a mocked HTTP client with an optional response body
        /// </summary>
        /// <param name="responseBody">
        /// The response body.
        /// </param>
        /// <returns>
        /// A <see cref="HttpClient"/>.
        /// </returns>
        public static HttpClient GetHttpClientMock(string responseBody = "")
        {
            var handler = new Mock<HttpMessageHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns(Task<HttpResponseMessage>.Factory.StartNew(() =>
                {
                    return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseBody) };
                })).Callback<HttpRequestMessage, CancellationToken>(
                    (_, _) =>
                    {
                    });

            return new HttpClient(handler.Object);
        }

        /// <summary>
        /// A mocked web server
        /// </summary>
        /// <param name="response">Function taking in a http request and returning content</param>
        /// <param name="certificate">Optional certificate to use</param>
        /// <returns>A http client that uses the webserver</returns>
        public static HttpClient GetHttpClientMock(Func<HttpRequestMessage, HttpResponseMessage> response, X509Certificate2 certificate = null)
        {
            var handler = new Mock<HttpClientHandler>();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .Returns<HttpRequestMessage, CancellationToken>((r, c) => Task<HttpResponseMessage>.Factory.StartNew(() =>
                {
                    var resp = response.Invoke(r);

                    if (resp == null)
                    {
                        throw new HttpRequestException("Connection refused");
                    }

                    if (c.IsCancellationRequested)
                    {
                        throw new TaskCanceledException();
                    }

                    return resp;
                }));

            return new HttpClient(handler.Object);
        }

        /// <summary>
        /// Get header from http request message
        /// </summary>
        /// <param name="request">The http request message</param>
        /// <param name="name">The name of the header</param>
        /// <returns>The content of the first matching header</returns>
        public static string GetHeader(this HttpRequestMessage request, string name)
        {
            var headers = request.Headers.FirstOrDefault(x => x.Key == name).Value;
            return headers.FirstOrDefault();
        }

        /// <summary>
        /// Get header from http response message
        /// </summary>
        /// <param name="response">The http response message</param>
        /// <param name="name">The name of the header</param>
        /// <returns>The content of the first matching header</returns>
        public static string GetHeader(this HttpResponseMessage response, string name)
        {
            var headers = response.Headers.FirstOrDefault(x => x.Key == name).Value;
            return headers.FirstOrDefault();
        }

        /// <summary>
        /// Get header from http content
        /// </summary>
        /// <param name="request">The http request message</param>
        /// <param name="name">The name of the header</param>
        /// <returns>The content of the first matching header</returns>
        public static string GetHeader(this HttpContent request, string name)
        {
            var headers = request.Headers.FirstOrDefault(x => x.Key == name).Value;
            return headers.FirstOrDefault();
        }

        /// <summary>
        /// Get header from http content
        /// </summary>
        /// <param name="request">The http request message</param>
        /// <param name="name">The name of the header</param>
        /// <returns>The content of the first matching header</returns>
        public static string GetHeader(this CacheSafeHttpContent request, string name)
        {
            var headers = request.Headers.FirstOrDefault(x => x.Key == name).Value;
            return headers.FirstOrDefault();
        }

        /// <summary>
        /// Get url parameters from a request
        /// </summary>
        /// <param name="request">The http request message</param>
        /// <param name="name">The name of the parameter</param>
        /// <returns>The content of the first matching parameter</returns>
        public static string GetParameter(this HttpRequestMessage request, string name)
        {
            var queryParams = request.RequestUri?.Query.Split('?').Last().Split('&').ToDictionary(x => x.Split('=').First(), x => x.Split('=').Skip(1).FirstOrDefault());
            var parameter = queryParams != null && queryParams.ContainsKey(name) ? queryParams[name] : null;
            return parameter;
        }
    }
}