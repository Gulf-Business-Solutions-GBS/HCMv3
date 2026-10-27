using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

/// <summary>
/// Self-service HR services: the employee's own certificate, letter and card-replacement requests, and
/// raising a new one.
///
/// <para>
/// There is intentionally no IT-service equivalent of this interface. HR services are something an employee
/// asks for about themselves; an IT service is raised on their behalf, has no legacy ESS screen, and is
/// submitted from its admin service instead.
/// </para>
/// </summary>
public interface IMyHRServiceAppService : IApplicationService
{
    Task<PagedResultDto<MyHRServiceRequestDto>> GetMyHRServices(GetMyRequestsInput input);

    /// <summary>Creates the record and, when the type is workflow-enabled, submits it in one step.</summary>
    Task<MyRequestSubmissionDto> RequestHRService(RequestHRServiceInput input);

    /// <summary>Withdraws a request while it is still theirs to withdraw.</summary>
    Task CancelMyHRService(EntityDto input);
}
