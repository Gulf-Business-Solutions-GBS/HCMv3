using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="TimeRegister"/>. <c>Id == null</c> means create; otherwise update.
/// No calculation logic is replicated here (none exists in the legacy source for this entity) -- only
/// required-field validation is enforced by <c>TimeRegistersAppService</c> (Employee/DayDate required, plus
/// the unique-(Employee, DayDate) check).
/// </summary>
public class CreateOrEditTimeRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime DayDate { get; set; }

    public int? DutyPeriodId { get; set; }

    public int? DutyId { get; set; }

    public int? TimePolicyId { get; set; }

    public DateTime ShiftStartTime1 { get; set; }

    public DateTime BreakStartTime { get; set; }

    public DateTime BreakEndTime { get; set; }

    public DateTime ShiftEndTime1 { get; set; }

    public DateTime ActualIn { get; set; }

    public DateTime BreakOut { get; set; }

    public DateTime BreakIn { get; set; }

    public DateTime ActualOut { get; set; }

    public DateTime ActualStartTime1 { get; set; }

    public DateTime ActualBreakStartTime { get; set; }

    public DateTime ActualBreakEndTime { get; set; }

    public DateTime ActualEndTime1 { get; set; }

    public double PlannedWorkingMins { get; set; }

    public double ActualWorkingMins { get; set; }

    public double TotalWorkingMins { get; set; }

    public double ActualAbsenceMins { get; set; }

    public double LeaveDay { get; set; }

    public int? LeaveTypeId { get; set; }

    public double LeaveMinutes { get; set; }

    public double AnnualLeaveDay { get; set; }

    public double UnpaidLeaveDay { get; set; }

    public double SickLeavePaidDay { get; set; }

    public double SickLeaveUnpaidDay { get; set; }

    public double PublicHoliday { get; set; }

    public double WeeklyOff { get; set; }

    public double RestDay { get; set; }

    public double AnnualLeaveAccrualPerDay { get; set; }

    public double TicketAccrualPerDay { get; set; }

    public double AirTicketPaidDay { get; set; }

    public double ActiveDay { get; set; }

    public double InactiveDay { get; set; }

    public double OvertimeMinutes { get; set; }

    public int? OvertimeTypeId { get; set; }

    public double PlannedWorkingDay { get; set; }

    public double ActualWorkingDay { get; set; }

    public double AttendanceDay { get; set; }

    public int? AttendanceTypeId { get; set; }

    public double AttendanceMinutes { get; set; }

    public double AbsentDay { get; set; }

    public double LateIn { get; set; }

    public double DeductionLateIn { get; set; }

    public double EarlyOut { get; set; }

    public double DeductionEarlyOut { get; set; }

    public double BreakEarlyOut { get; set; }

    public double BreakLateIn { get; set; }

    public double BreakDeductionEarlyOut { get; set; }

    public double BreakDeductionLateIn { get; set; }

    public double DedLateInEarlyOutMins { get; set; }

    public double DedLateInEarlyOutDay { get; set; }

    public double DedAfterMonthlyTol { get; set; }

    public double IncludeInLeavePay { get; set; }

    public double LeaveAdjDay { get; set; }

    public double ApprovedActualWorkingDay { get; set; }

    public double ApprovedAbsentDay { get; set; }

    public double ApprovedDedLateInEarlyOutMins { get; set; }

    public TimeRegisterStatus Status { get; set; }

    [StringLength(1000)]
    public string Remarks { get; set; }
}
