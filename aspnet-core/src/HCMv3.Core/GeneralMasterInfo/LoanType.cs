using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// LoanType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.LoanType</c>).
/// Extends the Code/NameEn/NameAr shape with loan eligibility/recovery fields, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The legacy <c>WageType</c>/<c>LoanDedWageType</c> fields are not ported (Payroll module out of scope);
/// the aggregated <c>LoanTypeDetail</c> collection is ported separately as its own entity (<see cref="LoanTypeDetail"/>).
/// </summary>
public class LoanType : FullAuditedEntity, IMustHaveTenant
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

    public virtual LoanDeductionType LoanDeductionType { get; set; }

    public virtual RecoveryOption RecoveryOption { get; set; }

    public virtual int EligiblePeriod { get; set; }

    public virtual int EligibleCountPerYear { get; set; }

    public virtual int MaxInstallments { get; set; }

    public virtual EligibleAmount EligibleAmount { get; set; }

    public virtual double EligibleAmountNumber { get; set; }
}
