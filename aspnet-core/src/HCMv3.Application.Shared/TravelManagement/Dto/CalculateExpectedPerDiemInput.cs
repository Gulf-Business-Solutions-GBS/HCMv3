namespace HCMv3.TravelManagement.Dto;

/// <summary>
/// Input for <c>TravelDetailsAppService.CalculateExpectedPerDiemAsync</c>.
///
/// DELIBERATE SIMPLIFICATION: the legacy lookup derived EmployeeGroup/EmployeeSubGroup automatically from
/// <c>Employee.OrgAssignment</c> as of DepartureDate. In this codebase,
/// <c>PersonnelAdministration.EmployeeOrgAssignment</c> does NOT carry EmployeeGroup/EmployeeSubGroup FKs --
/// they were explicitly deferred/not ported in that entity's own Wave B scope notes (see
/// <c>EmployeeOrgAssignment</c> class remarks), predating this TravelManagement port. Since that link does
/// not exist in this codebase, <see cref="EmployeeGroupId"/>/<see cref="EmployeeSubGroupId"/> must be
/// supplied explicitly by the caller instead of being auto-derived.
/// </summary>
public class CalculateExpectedPerDiemInput
{
    public int TravelDetailId { get; set; }

    public int EmployeeGroupId { get; set; }

    public int EmployeeSubGroupId { get; set; }
}
