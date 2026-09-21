using System.Net.Http;

namespace HCMv3.Tests;

/// <summary>
/// Stands in for the <see cref="IHttpClientFactory"/> that <c>AddHttpClient</c> registers in the web host.
///
/// Several domain services that talk to external systems -- the Integration Center connectors, the
/// time-attendance sync -- take the factory in their constructor, so without this they cannot even be
/// resolved in a test, let alone exercised. It hands out a real <see cref="HttpClient"/>: tests are
/// expected to stay on code paths that return before any request is made, and a genuine client fails
/// honestly if one ever does not, rather than throwing a null reference from a mock.
/// </summary>
public class TestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        return new HttpClient();
    }
}
