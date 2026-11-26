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

    /// <summary>
    /// Legacy OrgUnitCustomizeController: which optional fields (Position, Posting Group, Budgeting Group) this
    /// tenant shows on the org assignment screen.
    /// </summary>
    Task<EmployeeOrgAssignmentFieldSettingsDto> GetFieldSettings();

    /// <summary>
    /// Legacy OrgLevel1..OrgLevel7 pickers: every organization unit whose type is Level1..Level7, with its level
    /// and parent, so the screen can cascade one level from the one above. Captions come from
    /// <c>GridReportOrgLevels.GetOrgLevelCaptions</c>.
    /// </summary>
    Task<ListResultDto<OrgAssignmentOrgUnitDto>> GetOrgUnitsByLevel();
}
