using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TrainingManagement;

/// <summary>
/// Per-day attendance row for an Employee within a <see cref="TrainingSchedule"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TrainingManagement.TrainingAttendance</c>). Legacy
/// <c>PerformValidation</c> parity (TrainingSchedule/Employee/DayDate/Attendance required; DayDate must fall
/// within the parent TrainingSchedule's Start/EndDate; no duplicate Employee+TrainingSchedule+DayDate
/// combination) is enforced in <c>TrainingAttendancesAppService</c>, not here. <c>IExcelImport</c> is a
/// legacy XAF UI concept and is not ported.
/// </summary>
public class TrainingAttendance : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int TrainingScheduleId { get; set; }

    public virtual TrainingSchedule TrainingSchedule { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime DayDate { get; set; }

    public virtual TrainingAttendanceStatus Attendance { get; set; }
}
