using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Assigns an Employee to a <see cref="RosterDutySchedule"/> for a date range (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.EmployeeDutySchedule</c>, Wave 3).
///
/// DELIBERATE SIMPLIFICATION: the legacy entity also implements <c>IEvent</c> (a DevExpress Scheduler
/// calendar-widget adapter exposing Subject/Description/StartOn/EndOn/AllDay/Location/Label/Status/Type/
/// ResourceId/AppointmentId). That interface and all of its properties are XAF-UI-specific
/// calendar-widget plumbing with no equivalent needed in this Angular port and are not ported -- a normal
/// date-range table is sufficient. Only Employee/RosterDutySchedule/StartDate/EndDate are ported.
///
/// No EndDate-auto-chaining exists in the legacy file for this entity; an overlap check (no two schedules
/// for the same employee with overlapping date ranges) is ported in
/// <c>EmployeeDutySchedulesAppService</c>, following the same pattern used for <see cref="RosterGroupDetail"/>
/// (Wave 2). The legacy <c>IExcelImport</c> marker is an XAF UI concept and is not ported.
/// </summary>
public class EmployeeDutySchedule : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual int RosterDutyScheduleId { get; set; }

    public virtual RosterDutySchedule RosterDutySchedule { get; set; }

    public virtual DateTime StartDate { get; set; }

    public virtual DateTime EndDate { get; set; }
}
