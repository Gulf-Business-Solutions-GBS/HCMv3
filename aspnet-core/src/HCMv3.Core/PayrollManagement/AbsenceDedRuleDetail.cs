using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Date-range deduction step of an <see cref="AbsenceDeductionRule"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceDedRuleDetail</c>), listed separately and linked
/// back via <see cref="AbsenceDeductionRuleId"/>.
/// </summary>
public class AbsenceDedRuleDetail : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AbsenceDeductionRuleId { get; set; }

    public virtual AbsenceDeductionRule AbsenceDeductionRule { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }

    public virtual double DaysCount { get; set; }

    public virtual DeductionUnit DeductionUnit { get; set; }

    public virtual double DeductionNumber { get; set; }
}
