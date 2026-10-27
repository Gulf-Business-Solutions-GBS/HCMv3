using HCMv3.GeneralMasterInfo;

namespace HCMv3.SuccessionManagement;

/// <summary>
/// Talent pool master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SuccessionManagement.TalentPool</c>). The legacy type also implemented
/// <c>IExcelImport</c> (bulk Excel import wizard support) -- not ported, out of scope for the backend
/// app-service layer.
/// Pure Code/NameEn/NameAr lookup -- see <see cref="SimpleLookupEntity"/>.
/// </summary>
public class TalentPool : SimpleLookupEntity
{
}
