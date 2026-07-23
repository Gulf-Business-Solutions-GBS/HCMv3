using System;
using System.ComponentModel.DataAnnotations;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Manual timesheet entry for an Employee on a specific date (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.EmployeeTimesheet</c>, Wave 3). One row per
/// (Employee, TransDate) -- not a date-effective/StartDate-EndDate record, so no auto-chaining applies.
///
/// NOTE ON FIELD TYPES: in the legacy source, <c>ActualStartTime</c>/<c>BreakStartTime</c>/
/// <c>BreakEndTime</c>/<c>ActualEndTime</c> are declared as <c>DateTime</c> (a common legacy XPO pattern
/// for storing a bare time-of-day using a sentinel date). Here they are ported as <see cref="TimeSpan"/>,
/// matching the convention already established for <c>Duty.StartTime</c>/<c>Duty.EndTime</c> (Wave 1) --
/// this is a deliberate type simplification, not a literal port. <c>Approved</c> is a plain <c>int</c> in
/// the legacy source (no enum/flag definition found there) and is ported faithfully as <c>int</c>.
/// The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeTimesheet : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime TransDate { get; set; }

    public virtual TimeSpan ActualStartTime { get; set; }

    public virtual TimeSpan BreakStartTime { get; set; }

    public virtual TimeSpan BreakEndTime { get; set; }

    public virtual TimeSpan ActualEndTime { get; set; }

    public virtual double TotalHours { get; set; }

    public virtual int Approved { get; set; }

    [StringLength(500)]
    public virtual string Comments { get; set; }
}
