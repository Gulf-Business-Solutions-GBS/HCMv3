using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// AccountType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AccountType</c>). Pure Code/NameEn/NameAr lookup, so it
/// reuses the shared <see cref="SimpleLookupEntity"/> shape and <see cref="SimpleLookupAppServiceBase{TEntity}"/>
/// app-service infrastructure (same pattern as <c>TimeManagement.TimeType</c>). The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class AccountType : SimpleLookupEntity
{
}
