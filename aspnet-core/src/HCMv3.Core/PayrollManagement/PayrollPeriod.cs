using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// PayrollPeriod master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollPeriod</c>). Pure Code/NameEn/NameAr lookup, so
/// it reuses the shared <see cref="SimpleLookupEntity"/> shape and <see cref="SimpleLookupAppServiceBase{TEntity}"/>
/// app-service infrastructure (same pattern as <c>AccountType</c>). <see cref="SimpleLookupEntity.Code"/>
/// holds the fiscal-shift key (e.g. "Jan-Dec", "Feb-Jan" ... "Dec-Nov") consumed by
/// <see cref="PayrollControlRecord.GetStartEndDateFromPayrollPeriod"/>.
/// </summary>
public class PayrollPeriod : SimpleLookupEntity
{
}
