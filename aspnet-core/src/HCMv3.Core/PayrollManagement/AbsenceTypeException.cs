using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Records that a given <see cref="AbsenceType"/> is not compatible with another (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceTypeException</c>), listed separately and linked
/// back via <see cref="AbsenceTypeId"/>, following the flat parent+child pattern already used elsewhere
/// (e.g. TimeManagement's DutyPeriod/DutyPeriodDetail).
/// </summary>
public class AbsenceTypeException : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AbsenceTypeId { get; set; }

    public virtual AbsenceType AbsenceType { get; set; }

    public virtual int ExceptAbsenceTypeId { get; set; }

    public virtual AbsenceType ExceptAbsenceType { get; set; }
}
