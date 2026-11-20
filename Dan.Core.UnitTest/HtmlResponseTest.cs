using System.Diagnostics.CodeAnalysis;
using System.Net;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Middleware;
using Dan.Core.UnitTest.Helpers;

namespace Dan.Core.UnitTest;

[TestClass]
[ExcludeFromCodeCoverage]
public class HtmlResponseTest
{
    [TestMethod]
    public void CreateHtmlResponse_HtmlEncodesViewData()
    {
        var request = TestHelpers.CreateHttpRequestData();

        var response = request.CreateHtmlResponse(HttpStatusCode.OK, "Error.html", new { title = "Title", message = "<img src=x onerror=alert(1)>" });

        var body = TestHelpers.ReadBody(response);
        Assert.DoesNotContain("<img", body);
        Assert.Contains("&lt;img src=x onerror=alert(1)&gt;", body);
    }

    [TestMethod]
    public void CreateHtmlResponse_KeepsRawHtmlViewDataAsIs()
    {
        var request = TestHelpers.CreateHttpRequestData();

        var response = request.CreateHtmlResponse(HttpStatusCode.OK, "Error.html", new { title = "Title", message = new RawHtml("first<br />second") });

        var body = TestHelpers.ReadBody(response);
        Assert.Contains("first<br />second", body);
    }

    [TestMethod]
    public void HtmlExceptionHandler_ReturnsReasonAsPlainTextWithoutStackTrace()
    {
        var message = HtmlExceptionHandlerMiddleware.CreateMessage("<b>reason</b>", "stack", includeStackTrace: false);

        Assert.AreEqual("<b>reason</b>", message);
    }

    [TestMethod]
    public void HtmlExceptionHandler_HtmlEncodesReasonAndStackTraceWhenIncludingStackTrace()
    {
        var message = HtmlExceptionHandlerMiddleware.CreateMessage("<b>reason</b>", "at Foo<T>()", includeStackTrace: true);

        Assert.IsInstanceOfType<RawHtml>(message);
        Assert.AreEqual("&lt;b&gt;reason&lt;/b&gt;<br /><br/><pre>at Foo&lt;T&gt;()</pre>", message.ToString());
    }

    [TestMethod]
    public void CreateHtmlResponse_DoesNotExpandPlaceholdersInsideValues()
    {
        var request = TestHelpers.CreateHttpRequestData();

        var response = request.CreateHtmlResponse(HttpStatusCode.OK, "Error.html", new { title = "SECRET-TITLE", message = "see @title" });

        var body = TestHelpers.ReadBody(response);
        Assert.Contains("see @title", body);
    }

    [TestMethod]
    public void CreateHtmlResponse_SetsContentSecurityPolicy()
    {
        var request = TestHelpers.CreateHttpRequestData();

        var response = request.CreateHtmlResponse(HttpStatusCode.OK, "Error.html", new { title = "Title", message = "Message" });

        Assert.IsTrue(response.Headers.TryGetValues("Content-Security-Policy", out var values));
        var policy = values.Single();
        Assert.Contains("default-src 'none'", policy);
        Assert.Contains("form-action 'none'", policy);
        Assert.Contains("base-uri 'none'", policy);
        Assert.Contains("frame-ancestors 'none'", policy);
    }

    [TestMethod]
    public void CreateHtmlResponse_DisablesContentTypeSniffing()
    {
        var request = TestHelpers.CreateHttpRequestData();

        var response = request.CreateHtmlResponse(HttpStatusCode.OK, "Error.html", new { title = "Title", message = "Message" });

        Assert.IsTrue(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.AreEqual("nosniff", values.Single());
    }
}
