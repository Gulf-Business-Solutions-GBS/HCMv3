namespace HCMv3.Careers;

/// <summary>
/// Supplies the host name of the current inbound request (e.g. <c>acme.careers.example.com</c>) to the
/// careers public surface.
///
/// This exists purely to keep <see cref="ICareersTenantResolver"/>'s logic -- which is the security-critical
/// part of the public careers portal -- inside the Application layer where it is unit-testable, rather than
/// in a Web project that <c>HCMv3.Tests</c> does not reference. The only web-specific concern (reading
/// <c>HttpContext.Request.Host.Host</c>) is isolated behind this one-method abstraction and implemented by
/// <c>HCMv3.Web.Careers.HttpCareersHostNameProvider</c> in <c>HCMv3.Web.Core</c>.
/// </summary>
public interface ICareersHostNameProvider
{
    /// <summary>
    /// The host name (no scheme, no port) of the current request, or <c>null</c> when there is no ambient
    /// HTTP request (background jobs, tests).
    /// </summary>
    string GetHostNameOrNull();
}
