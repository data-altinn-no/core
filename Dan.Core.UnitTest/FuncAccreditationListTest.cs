using System.Diagnostics.CodeAnalysis;
using AwesomeAssertions;
using Dan.Common.Models;
using Dan.Core;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using FakeItEasy;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dan.Core.UnitTest;

[TestClass]
[ExcludeFromCodeCoverage]
public class FuncAccreditationListTest
{
    private readonly IRequestContextService _mockRequestContextService = A.Fake<IRequestContextService>();
    private readonly IAccreditationRepository _mockAccreditationRepository = A.Fake<IAccreditationRepository>();
    private readonly IEvidenceStatusService _mockEvidenceStatusService = A.Fake<IEvidenceStatusService>();
    private readonly ILoggerFactory _loggerFactory = new NullLoggerFactory();

    [TestMethod]
    public async Task RunAsync_QueriesRepository_ScopedToAuthenticatedServiceContextAndOwner()
    {
        const string serviceContextName = "the-service-context";
        const string ownerOrgNumber = "123456789";

        A.CallTo(() => _mockRequestContextService.ServiceContext)
            .Returns(new ServiceContext { Name = serviceContextName });
        A.CallTo(() => _mockRequestContextService.AuthenticatedOrgNumber)
            .Returns(ownerOrgNumber);
        A.CallTo(() => _mockAccreditationRepository.QueryAccreditationsAsync(A<AccreditationsQuery>._, A<string>._))
            .Returns(new List<Accreditation>());

        var func = new FuncAccreditationList(
            _mockRequestContextService,
            _mockAccreditationRepository,
            _mockEvidenceStatusService,
            _loggerFactory);

        var request = CreateHttpRequestData();

        await func.RunAsync(request);

        A.CallTo(() => _mockAccreditationRepository.QueryAccreditationsAsync(
                A<AccreditationsQuery>.That.Matches(q => q.ServiceContext == serviceContextName),
                ownerOrgNumber))
            .MustHaveHappenedOnceExactly();
    }

    private static HttpRequestData CreateHttpRequestData(string query = "")
    {
        var functionContext = A.Fake<FunctionContext>();

        var request = A.Fake<HttpRequestData>(options =>
            options.WithArgumentsForConstructor(new object[] { functionContext }));
        A.CallTo(() => request.Url).Returns(new Uri($"https://localhost/api/accreditations{query}"));

        var response = A.Fake<HttpResponseData>(options =>
            options.WithArgumentsForConstructor(new object[] { functionContext }));
        response.Headers = new HttpHeadersCollection();
        response.Body = new MemoryStream();

        A.CallTo(() => request.CreateResponse()).Returns(response);

        return request;
    }
}
