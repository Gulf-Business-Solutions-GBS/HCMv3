using System;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.TimeManagement.Dto;

/// <summary>
/// Create/Update input DTO for <see cref="MonthlyTimeRegister"/>. <c>Id == null</c> means create; otherwise
/// update.
/// </summary>
public class CreateOrEditMonthlyTimeRegisterDto
{
    public int? Id { get; set; }

    [Required]
    public int EmployeeId { get; set; }

    [Required]
    public DateTime Month { get; set; }

    public double AnnualLeaveDays { get; set; }

    public double UnpaidLeaveDays { get; set; }

    public double SickLeavePaidDays { get; set; }

    public double SickLeaveUnpaidDays { get; set; }

    public double GratuityDays { get; set; }

    public double PublicHolidays { get; set; }

    public double WeeklyOffs { get; set; }

    public double RestDays { get; set; }

    public double AnnualLeaveAccrualDays { get; set; }

    public double AnnualLeaveOpeningBal { get; set; }

    public double AnnualLeaveClosingBal { get; set; }

    public double TicketAccrualDays { get; set; }

    public double TicketAccrualOpeningBal { get; set; }

    public double TicketAccrualClosingBal { get; set; }

    public double AirTicketPaidDays { get; set; }

    public double GratuityPaidDays { get; set; }

    public double ActiveDays { get; set; }

    public double AdvanceLeavePaids { get; set; }

    public double AdvanceTicketPaids { get; set; }

    public double PlannedWorkingMins { get; set; }

    public double ActualWorkingMins { get; set; }

    public double ActualAbsenceMins { get; set; }

    public double NormalDayOTMins { get; set; }

    public double RestDayOTMins { get; set; }

    public double HolidayOTMins { get; set; }

    public double WeekendOTMins { get; set; }

    public double PlannedWorkingDays { get; set; }

    public double ActualWorkingDays { get; set; }

    public double ExtraWorkingDays { get; set; }

    public double LieuDays { get; set; }

    public double AdvLeaveRecoveryDays { get; set; }
}
