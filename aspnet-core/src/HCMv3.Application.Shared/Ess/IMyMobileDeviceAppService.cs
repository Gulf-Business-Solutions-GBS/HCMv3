using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service mobile device registration: an employee telling HR which handset the mobile app should
/// accept their attendance punches from.
///
/// <para>
/// This is where legacy actually created these -- the mobile app posted to
/// <c>WorkflowServiceApiController</c>, which built a <c>MobileAppDeviceRequest</c> and appended
/// "(Submitted Via Mobile App)" to the comments. The employee registers their own phone, so the employee
/// comes from the session and there is no employee field anywhere on this contract.
/// </para>
///
/// <para>
/// There is no <c>MobileAppDeviceRequest</c> table here -- the registration row IS the request, hidden by the
/// <c>ApprovedOnly</c> filter until it is granted. In legacy nothing existed for an approved request to
/// become, and nothing ever read <c>DeviceId</c> back; here approval is exactly what makes the device
/// answerable to <c>IMobileDeviceRegistrationService</c>.
/// </para>
/// </summary>
public interface IMyMobileDeviceAppService : IApplicationService
{
    /// <summary>The caller's own registered and pending devices.</summary>
    Task<PagedResultDto<MyMobileDeviceDto>> GetMyMobileDevices(GetMyRequestsInput input);

    /// <summary>Registers the handset and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RegisterMyMobileDevice(RegisterMyMobileDeviceInput input);

    /// <summary>Withdraws the request while it is still theirs to withdraw.</summary>
    Task CancelMyMobileDevice(EntityDto input);
}
