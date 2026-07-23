using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Simple link between an <see cref="AbsenceType"/> and the <see cref="WageType"/> used to pay it (ported
/// from legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsencePayment</c>).
///
/// DELIBERATE FIX (not a straight port): the legacy <c>OnSaving</c> has its <c>PerformValidation()</c> call
/// literally commented out, meaning duplicate (AbsenceType, WageType) rows are technically allowed in the
/// legacy system. This is judged an obvious oversight rather than intended behavior, so this port ADDS a
/// proper duplicate-prevention check in <c>AbsencePaymentsAppService</c> instead of replicating the gap.
/// </summary>
public class AbsencePayment : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual int WageTypeId { get; set; }

    public virtual WageType WageType { get; set; }
}
