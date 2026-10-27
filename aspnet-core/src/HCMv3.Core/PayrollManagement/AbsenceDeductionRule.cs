using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Absence deduction rule header (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceDeductionRule</c>). Unlike most masters in this
/// module it has only a single English name field (<see cref="RuleName"/>), not the usual NameEn/NameAr
/// pair, so it can't derive from <see cref="SimpleLookupEntity"/>.
/// </summary>
public class AbsenceDeductionRule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    [StringLength(SimpleLookupConsts.MaxCodeLength)]
    public virtual string Code { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public virtual string RuleName { get; set; }
}
