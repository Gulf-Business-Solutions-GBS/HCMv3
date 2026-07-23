namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Ported from legacy XAF <c>HCM10.Module.BusinessObjects.PersonnelAdministration.DocumentStatus</c>.
/// Computed (not stored) for <see cref="EmployeeDocument"/> -- see <c>EmployeeDocumentsAppService</c>.
/// </summary>
public enum DocumentStatus
{
    Inactive = 0,
    Active = 1,
    Expired = 2
}
