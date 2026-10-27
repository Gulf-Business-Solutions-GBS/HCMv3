using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// PayrollReasonType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.PayrollReasonType</c>). Pure Code/NameEn/NameAr lookup,
/// so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure (same pattern as
/// <c>TimeManagement.TimeType</c>). Used later by <c>SalaryRegister.PayrollReasonType</c>.
/// </summary>
public class PayrollReasonType : SimpleLookupEntity
{
}
