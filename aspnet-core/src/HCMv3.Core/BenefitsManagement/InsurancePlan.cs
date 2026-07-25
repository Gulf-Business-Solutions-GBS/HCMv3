using HCMv3.GeneralMasterInfo;

namespace HCMv3.BenefitsManagement;

/// <summary>
/// Insurance plan lookup (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.BenefitsManagement.InsurancePlan</c>). Pure Code/NameEn/NameAr
/// lookup, so it reuses <see cref="SimpleLookupEntity"/> -- legacy's <c>PerformValidation</c>
/// (auto-code, duplicate code/name checks) is handled generically by
/// <c>SimpleLookupAppServiceBase{TEntity}</c>. <c>IExcelImport</c> is an XAF UI concept and is not ported.
/// </summary>
public class InsurancePlan : SimpleLookupEntity
{
}
