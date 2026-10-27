using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

/// <summary>
/// TravelCancelReason master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TravelManagement.TravelCancelReason</c>). Pure Code/NameEn/NameAr lookup,
/// no FK, so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure. Not referenced by any other
/// ported entity in this wave (the legacy <c>Travel</c> object has no cancel-reason field) -- it is ported as
/// a standalone master list only. The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not
/// ported.
/// </summary>
public class TravelCancelReason : SimpleLookupEntity
{
}
