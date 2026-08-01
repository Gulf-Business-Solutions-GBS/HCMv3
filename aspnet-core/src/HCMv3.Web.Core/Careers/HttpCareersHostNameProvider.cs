using Abp.Dependency;
using HCMv3.Careers;
using Microsoft.AspNetCore.Http;

namespace HCMv3.Web.Careers;

/// <summary>
/// The only web-specific piece of careers tenant resolution: hands the current request's host name to
/// <c>HCMv3.Careers.CareersTenantResolver</c>, which lives in the Application layer so its logic stays
/// unit-testable (HCMv3.Tests does not reference HCMv3.Web.Core).
///
/// Registered by convention (<see cref="ITransientDependency"/>) when HCMv3.Web.Core loads, which both
/// HCMv3.Web.Host and HCMv3.Web.Public depend on.
/// </summary>
public class HttpCareersHostNameProvider : ICareersHostNameProvider, ITransientDependency
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCareersHostNameProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string GetHostNameOrNull()
    {
        // Host.Host is the host name without the port. Null outside an HTTP request (background jobs).
        return _httpContextAccessor.HttpContext?.Request?.Host.Host;
    }
}
