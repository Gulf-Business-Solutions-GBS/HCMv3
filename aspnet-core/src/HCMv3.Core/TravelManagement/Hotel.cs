using HCMv3.GeneralMasterInfo;

namespace HCMv3.TravelManagement;

/// <summary>
/// Hotel master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.TravelManagement.Hotel</c>). Pure
/// Code/NameEn/NameAr lookup, no FK, so it reuses the shared <see cref="SimpleLookupEntity"/> shape and
/// <see cref="SimpleLookupAppServiceBase{TEntity}"/> app-service infrastructure. The legacy
/// <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class Hotel : SimpleLookupEntity
{
}
