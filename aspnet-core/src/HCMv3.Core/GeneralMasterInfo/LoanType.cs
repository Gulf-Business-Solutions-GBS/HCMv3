using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PayrollManagement;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// LoanType master (ported from legacy XAF <c>HCM10.Module.BusinessObjects.GeneralMasterInfo.LoanType</c>).
/// Extends the Code/NameEn/NameAr shape with loan eligibility/recovery fields, so it can't derive from
/// <see cref="SimpleLookupEntity"/> (see <see cref="City"/> for the same bespoke pattern).
/// The aggregated <c>LoanTypeDetail</c> collection is ported separately as its own entity
/// (<see cref="LoanTypeDetail"/>).
///
/// WAVE P9: the legacy <c>WageType</c> FK -- originally dropped as "Payroll module out of scope" -- is
/// RESTORED as <see cref="WageTypeId"/>, for the same reason <see cref="OvertimeType.WageTypeId"/> was
/// restored in wave P8. It is the ONLY thing that routes wage code 2001 "Loan Recovery Deduction" to a
/// calculation: legacy's wage-code list has <c>"2001"</c> COMMENTED OUT (legacy
/// <c>HCM10.PayrollEngine/Program.cs</c> line 2691) and instead adds every <c>LoanType.WageType.Oid</c> to
/// <c>wtCalculatedWageList</c> (legacy <c>Program.cs</c> line 2700), then dispatches through
/// <c>Program.LoanTypeList.Where(r =&gt; r.WageType.Oid == currWageType.Oid)</c> (legacy
/// <c>EmployeeCalculatedPayService.cs</c> line 73). Without this column the deduction is never posted.
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

    /// <summary>
    /// Legacy <c>LoanType.WageType</c> (legacy <c>GeneralMasterInfo/LoanType.cs</c>, the
    /// <c>[DataSourceProperty("LoanDedWageType")]</c> member). The wage type the loan recovery deduction is
    /// posted against -- in the reference database all four loan types point at wage type code 2001.
    ///
    /// <para>Nullable: legacy's own queries guard with <c>Where(r =&gt; r.WageType != null)</c> (legacy
    /// <c>Program.cs</c> line 2671), so a loan type with no wage type is a legitimate state -- it simply
    /// never produces a deduction line.</para>
    /// </summary>
    public virtual int? WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
