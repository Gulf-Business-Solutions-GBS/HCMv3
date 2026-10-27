using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Read-only feeds for the Employee Workbench that no existing per-employee service already answers.
/// Everything else on the workbench -- the "last 5" grids and every create or edit -- goes through the
/// existing per-entity services and their forms, so validation lives in one place.
/// </summary>
public interface IEmployeeWorkbenchAppService : IApplicationService
{
    Task<ListResultDto<WorkbenchEmployeeSearchItemDto>> SearchEmployees(SearchWorkbenchEmployeesInput input);

    Task<EmployeeWorkbenchHeaderDto> GetHeader(EntityDto input);

    Task<ListResultDto<WorkbenchNotReturnedLeaveDto>> GetNotReturnedLeaves(EntityDto input);

    Task<ListResultDto<WorkbenchAttendanceDayDto>> GetRecentAttendance(EntityDto input);
}
