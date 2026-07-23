using System;
using Abp.Application.Services.Dto;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Read/list DTO for <see cref="TimeRegister"/>. See the entity class remarks for the fields/simplifications
/// that were dropped or renamed relative to the legacy source.
/// </summary>
public class TimeRegisterDto : EntityDto<int>
{
    public int EmployeeId { get; set; }

    public string EmployeeCode { get; set; }

    public string EmployeeFullNameEn { get; set; }

    public DateTime DayDate { get; set; }

    public int? DutyPeriodId { get; set; }

    public string DutyPeriodName { get; set; }

    public int? DutyId { get; set; }

    public string DutyName { get; set; }

    public int? TimePolicyId { get; set; }

    public string TimePolicyName { get; set; }

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

    public string OvertimeTypeName { get; set; }

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

    public string Remarks { get; set; }
}
