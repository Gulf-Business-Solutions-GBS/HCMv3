using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Per-employee exception (date-range exclusion) for an <see cref="AbsenceViolationRule"/> (ported from
/// legacy XAF <c>HCM10.Module.BusinessObjects.PayrollManagement.AbsenceViolationRuleException</c>), listed
/// separately and linked back via <see cref="AbsenceViolationRuleId"/>.
/// </summary>
public class AbsenceViolationRuleException : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int AbsenceViolationRuleId { get; set; }

    public virtual AbsenceViolationRule AbsenceViolationRule { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
