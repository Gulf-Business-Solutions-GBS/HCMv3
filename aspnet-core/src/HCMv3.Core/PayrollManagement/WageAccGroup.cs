using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// WageAccGroup master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.WageAccGroup</c>). Pure Code/NameEn/NameAr lookup, so
/// it reuses the shared <see cref="SimpleLookupEntity"/> shape and <see cref="SimpleLookupAppServiceBase{TEntity}"/>
/// app-service infrastructure. Its child rows (<see cref="WageAccGroupDetail"/>) are listed/edited
/// separately, same pattern as the EmployeeSubGroup* children in prior waves. The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class WageAccGroup : SimpleLookupEntity
{
}
