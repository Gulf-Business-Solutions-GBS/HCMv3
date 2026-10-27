using HCMv3.GeneralMasterInfo;

namespace HCMv3.TimeManagement;

/// <summary>
/// TimeTransferType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TimeManagement.TimeTransferType</c>).
/// Pure Code/NameEn/NameAr lookup shape, so it reuses <see cref="SimpleLookupEntity"/> for the entity shape.
/// Unlike the other pure lookups, the legacy <c>OnDeleting</c> unconditionally blocks deletion
/// ("You can't delete record. Please contact administrator."), so its app service
/// (<see cref="TimeTransferTypesAppService"/>) is bespoke rather than deriving
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> (which has no delete-blocking hook).
/// </summary>
public class TimeTransferType : SimpleLookupEntity
{
}
