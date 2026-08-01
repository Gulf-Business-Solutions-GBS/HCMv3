using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.Careers.Dto;

namespace HCMv3.Careers;

/// <summary>
/// The PUBLIC (anonymous) candidate-identity surface of the careers portal: register, forgot password,
/// reset password. Wave R2.
///
/// LOGIN IS DELIBERATELY ABSENT from this interface. Candidates are ordinary ABP users, so they sign in
/// through the stock <c>/api/TokenAuth/Authenticate</c> endpoint and get the whole ASP.NET Zero login
/// pipeline (password hashing, failed-attempt lockout, two-factor, session management, login-attempt
/// auditing). What makes that endpoint land on the right tenant when it is called on a careers subdomain is
/// <c>HCMv3.Careers.CareersTenantResolveContributor</c>, NOT a second login implementation here. Writing a
/// bespoke careers login would have meant re-deriving token issuance and lockout, which is exactly the sort
/// of parallel auth path this wave exists to delete.
///
/// Like <see cref="ICareersAppService"/>, every method resolves its tenant from the request subdomain via
/// <see cref="ICareersTenantResolver"/> and fails closed. There is no host/null fallback.
/// </summary>
public interface ICareersAccountAppService : IApplicationService
{
    Task<CareersRegisterOutput> Register(CareersRegisterInput input);

    /// <summary>
    /// Mails a password reset link. Returns <c>void</c> and behaves identically whether or not the address
    /// exists, on purpose -- see the implementation remarks.
    /// </summary>
    Task SendPasswordResetCode(CareersSendPasswordResetCodeInput input);

    Task<CareersResetPasswordOutput> ResetPassword(CareersResetPasswordInput input);
}
