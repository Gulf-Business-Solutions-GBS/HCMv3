using HCMv3.GeneralMasterInfo;

namespace HCMv3.ProjectManagement;

/// <summary>
/// ProjectType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.ProjectManagement.ProjectType</c>). Pure Code/NameEn/NameAr lookup, no
/// FK, so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure (same pattern as
/// <c>PayrollManagement.AccountType</c>). The legacy <c>IExcelImport</c> marker is an XAF UI concept and is
/// not ported.
/// </summary>
public class ProjectType : SimpleLookupEntity
{
}
