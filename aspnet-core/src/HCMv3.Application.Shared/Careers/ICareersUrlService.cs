namespace HCMv3.Careers;

/// <summary>
/// Builds the email-activation and password-reset link FORMATS that get mailed to careers-portal
/// candidates.
///
/// Why this is not <c>HCMv3.Url.IAppUrlService</c>: that service builds links against
/// <c>IWebUrlService.GetSiteRootAddress</c>, i.e. the Angular ADMIN application
/// (<c>HCMv3.Web.Url.AngularAppUrlService</c> -> <c>account/confirm-email</c> on the admin host). Mailing a
/// candidate an admin-app link would drop them on a sign-in page for a product they have no account on, and
/// -- worse -- would carry a valid reset code onto a host they were never meant to touch. Candidate links
/// must point at the candidate's own tenant's careers subdomain.
///
/// Returned formats use the same placeholder tokens <c>HCMv3.Authorization.Users.UserEmailer</c> already
/// substitutes (<c>{userId}</c>, <c>{confirmationCode}</c>, <c>{resetCode}</c>, <c>{expireDate}</c>,
/// <c>{tenantId}</c>), so the stock emailer is reused verbatim.
/// </summary>
public interface ICareersUrlService
{
    /// <summary>
    /// Email-activation link format for the given tenant's careers subdomain, or <c>null</c> when the
    /// careers domain is not configured. <c>null</c> is a valid input to <c>UserEmailer</c> methods that
    /// accept an optional link and causes the activation email to be skipped rather than to be sent
    /// pointing somewhere wrong.
    /// </summary>
    string CreateEmailActivationUrlFormatOrNull(string tenancyName);

    /// <summary>
    /// Password-reset link format for the given tenant's careers subdomain, or <c>null</c> when the careers
    /// domain is not configured.
    /// </summary>
    string CreatePasswordResetUrlFormatOrNull(string tenancyName);
}
