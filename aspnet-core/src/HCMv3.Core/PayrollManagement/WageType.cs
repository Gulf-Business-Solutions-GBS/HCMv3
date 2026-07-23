using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// WageType master (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.WageType</c>) — the core Payroll wage-type
/// configuration entity, ported field-by-field faithfully.
///
/// DELIBERATE SIMPLIFICATIONS / BUSINESS-RULE NOTES (enforced in <c>WageTypesAppService</c>, not here):
/// <list type="bullet">
/// <item><description>Unlike most Code/Name lookups in this port, the legacy <c>Code</c> is required but is
/// NOT auto-generated (no <c>GetMaxCode</c> fallback exists in the legacy <c>PerformValidation</c>) — the
/// caller must always supply it.</description></item>
/// <item><description>On CREATE only, <see cref="BillingType"/> == TimeType or Provision is blocked
/// (reserved for internal/system use, legacy <c>PerformValidation</c> parity).</description></item>
/// <item><description><see cref="PayrollOrder"/> must be non-zero (legacy required-non-zero
/// parity). The legacy's commented-out/dead PayrollOrder-uniqueness and WageTypeException-cross-check
/// blocks are NOT ported (already dead code in the legacy source).</description></item>
/// <item><description>DELETE is blocked when <see cref="Category"/> == Technical (legacy
/// <c>OnDeleting</c> parity).</description></item>
/// </list>
/// </summary>
public class WageType : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    /// <summary>
    /// Required, immutable once set, and NOT auto-generated (unlike most other lookups in this port) — see
    /// class remarks.
    /// </summary>
    [Required]
    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameEn { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string NameAr { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string ShortNameEn { get; set; }

    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string ShortNameAr { get; set; }

    public virtual BillingType BillingType { get; set; }

    public virtual BillingMethod BillingMethod { get; set; }

    public virtual ProrateMethod ProrateMethod { get; set; }

    public virtual double FixedDays { get; set; }

    public virtual double Rate { get; set; }

    /// <summary>Legacy display name "Active".</summary>
    public virtual bool Status { get; set; }

    public virtual Category Category { get; set; }

    /// <summary>
    /// Self-referencing FK: the WageType this one arrears from.
    /// </summary>
    public virtual int? ArrearSourceWageTypeId { get; set; }

    public virtual WageType ArrearSourceWageType { get; set; }

    /// <summary>
    /// Order for Payroll to Calculate. Required to be non-zero.
    /// </summary>
    public virtual int PayrollOrder { get; set; }

    /// <summary>
    /// When true, restricts access to this wage type to the roles listed in
    /// <see cref="WageTypeSecurityRole"/> (no filtering logic is implemented yet in this wave — the join
    /// table only).
    /// </summary>
    public virtual bool IsSecured { get; set; }

    public virtual bool IncludeInIndemnity { get; set; }

    public virtual bool IncludeInPaidLeave { get; set; }

    public virtual bool IncludeInBasicPay { get; set; }

    public virtual bool IncludeInArrearPay { get; set; }

    public virtual bool IncludeInCalculatedPay { get; set; }

    public virtual bool IncludeInAdvancePay { get; set; }

    public virtual bool IncludeInOvertimePay { get; set; }

    public virtual bool IncludeInFIPosting { get; set; }

    public virtual bool IncludeInCutoff { get; set; }

    public virtual bool IncludeInDisciplinary { get; set; }

    public virtual bool IncludeInVariablePay { get; set; }

    public virtual bool IncludeInSocialSecurity { get; set; }

    public virtual bool IncludeInPartialPay { get; set; }

    public virtual bool IncludeInLateEarlyDed { get; set; }

    public virtual bool IncludeInServiceBonus { get; set; }

    public virtual bool IncludeInFund { get; set; }

    public virtual bool DontImpactNetPay { get; set; }
}
