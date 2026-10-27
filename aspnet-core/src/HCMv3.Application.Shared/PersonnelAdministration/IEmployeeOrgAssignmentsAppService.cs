using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.PersonnelAdministration.Dto;

namespace HCMv3.PersonnelAdministration;

public interface IEmployeeOrgAssignmentsAppService : IApplicationService
{
    Task<PagedResultDto<EmployeeOrgAssignmentDto>> GetAll(GetAllEmployeeOrgAssignmentsInput input);

    Task<GetEmployeeOrgAssignmentForEditOutput> GetEmployeeOrgAssignmentForEdit(EntityDto input);

    Task CreateOrEdit(CreateOrEditEmployeeOrgAssignmentDto input);

    Task Delete(EntityDto input);

    /// <summary>
    /// Legacy position picker: active, vacant positions (plus the one the assignment already holds).
    /// </summary>
    Task<ListResultDto<PositionForOrgAssignmentDto>> GetPositionsForOrgAssignment(GetPositionsForOrgAssignmentInput input);
}
