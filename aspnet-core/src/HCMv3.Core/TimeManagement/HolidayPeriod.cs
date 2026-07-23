using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;

namespace HCMv3.TimeManagement;

/// <summary>
/// Holiday Period (dated occurrence of a <see cref="Holiday"/>) — ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.HolidayPeriod</c>.
///
/// DELIBERATE SIMPLIFICATION: the legacy entity's <c>OnSaving</c>/<c>OnDeleting</c> pipeline walks
/// <c>EmployeeAbsence</c>, adjusts <c>EmployeeAbsenceQuota</c>, and triggers <c>EmployeePayrollStatus</c>
/// retro-calculation and <c>Employee.ProbationEndDate</c> recalculation. None of that cascade is ported —
/// those entities are out of scope for this wave (leave/quota/payroll modules not yet ported). Only the
/// basic shape (FK + date) and its uniqueness validation are ported here.
/// </summary>
public class HolidayPeriod : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int HolidayId { get; set; }

    public virtual Holiday Holiday { get; set; }

    public virtual DateTime HolidayDate { get; set; }
}
