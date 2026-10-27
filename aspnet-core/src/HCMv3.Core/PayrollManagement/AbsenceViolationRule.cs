using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Absence violation rule master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceViolationRule</c>).
/// Pure Code/NameEn/NameAr lookup, so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure (same pattern as
/// <c>TimeManagement.TimeType</c>). The legacy <c>Detail</c> / <c>Exception</c> aggregated collections are
/// ported as the separately-listed <see cref="AbsenceViolationRuleDetail"/> / <see cref="AbsenceViolationRuleException"/>
/// entities, linked back by FK, following the flat parent+child pattern used elsewhere in this codebase.
/// </summary>
public class AbsenceViolationRule : SimpleLookupEntity
{
}
