using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// TransType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TransType</c>).
/// Pure Code/NameEn/NameAr lookup, so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure (see <see cref="TimeType"/>).
/// </summary>
public class TransType : SimpleLookupEntity
{
}
