using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// TimeType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TimeType</c>).
/// Pure Code/NameEn/NameAr lookup, so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure already established for
/// the GeneralMasterInfo lookups (e.g. Country) rather than duplicating the shape here.
/// </summary>
public class TimeType : SimpleLookupEntity
{
}
