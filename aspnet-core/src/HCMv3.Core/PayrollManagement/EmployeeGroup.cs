using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// EmployeeGroup master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.EmployeeGroup</c>).
///
/// DELIBERATE SIMPLIFICATION: the legacy explicitly-dead <c>#region &lt;&lt; NotUsed &gt;&gt;</c> fields
/// (<c>RateCalcMethod</c>, <c>DaysPerMonth</c>, <c>LeaveSwitchPeriod</c>, <c>GratuitySwitchPeriod</c>,
/// <c>GratuityDeductPaidDays</c>, <c>GratuityDeductUnpaidDays</c>) are confirmed unused in the legacy source
/// itself and are NOT ported. The legacy <c>EmployeeSubGroups</c> aggregated collection and <c>IExcelImport</c>
/// marker are also not ported (EmployeeSubGroup doesn't exist in this port; IExcelImport is an XAF UI
/// concept). NOTE: <c>HCMv3.PersonnelAdministration.EmployeeOrgAssignment</c> has no
/// EmployeeGroupId/EmployeeSubGroupId/PayrollAreaId fields yet — wiring EmployeeGroup into the org
/// assignment is a separate future integration step, not part of this wave.
/// </summary>
public class EmployeeGroup : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    public virtual double IndemnityMaxNumber { get; set; }

    public virtual Unit IndemnityMaxUnit { get; set; }

    public virtual double IndemnityMaxAccruePercentage { get; set; }
}
