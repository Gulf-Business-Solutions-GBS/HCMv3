using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.TimeManagement.Dto;

namespace HCMv3.TimeManagement;

/// <summary>
/// The manager/HR screen that reviews the time engine's calculated days and puts corrections forward for
/// approval (legacy <c>TimeLogRequest</c> / <c>TimeLogRepository</c>). Not self-service: there is no ESS
/// counterpart and the workflow catalog row has no ESS route.
/// </summary>
public interface ITimeLogApprovalsAppService : IApplicationService
{
    Task<List<TimeLogApprovalGridRowDto>> GetTimeLogApprovalGrid(GetTimeLogApprovalGridInput input);

    Task<SubmitTimeLogApprovalResultDto> SubmitTimeLogApproval(SubmitTimeLogApprovalDto input);
}
