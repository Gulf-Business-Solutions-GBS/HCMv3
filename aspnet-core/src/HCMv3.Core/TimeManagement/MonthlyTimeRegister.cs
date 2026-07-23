using System;
using Abp.Domain.Entities;
using Abp.Domain.Entities.Auditing;
using HCMv3.PersonnelAdministration;

namespace HCMv3.TimeManagement;

/// <summary>
/// Monthly aggregate of <see cref="TimeRegister"/> data per Employee (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.TimeManagement.MonthlyTimeRegister</c>, Wave 4). Unique per
/// (Employee, Month).
///
/// CORRECTION vs. the task's pre-read field guesses: <c>Month</c> is a legacy <c>DateTime</c> (not an
/// <c>int</c>) -- ported faithfully as <see cref="DateTime"/> (first-of-month convention, as in the legacy
/// source). The legacy field <c>TicketAccrualDaya</c> is a typo (should be "Days") -- fixed here to
/// <see cref="TicketAccrualDays"/> since renaming it does not affect any calculation logic (none exists in
/// the legacy source for this entity) and there is no other code in this port that depends on the misspelled
/// name.
///
/// The legacy <c>OnSaving</c> silently swallows exceptions (<c>catch (Exception ex) { }</c> with no rethrow)
/// -- this is a real legacy bug and is NOT replicated; validation exceptions in
/// <c>MonthlyTimeRegistersAppService</c> propagate normally as <c>UserFriendlyException</c>.
/// </summary>
public class MonthlyTimeRegister : FullAuditedEntity, IMustHaveTenant
{
    public int TenantId { get; set; }

    public virtual int EmployeeId { get; set; }

    public virtual Employee Employee { get; set; }

    public virtual DateTime Month { get; set; }

    public virtual double AnnualLeaveDays { get; set; } // TimeType = 1

    public virtual double UnpaidLeaveDays { get; set; } // TimeType = 2

    public virtual double SickLeavePaidDays { get; set; } // TimeType = 3

    public virtual double SickLeaveUnpaidDays { get; set; } // TimeType = 5

    public virtual double GratuityDays { get; set; } // TimeType = 6

    public virtual double PublicHolidays { get; set; } // TimeType = 7

    public virtual double WeeklyOffs { get; set; } // TimeType = 8

    public virtual double RestDays { get; set; } // TimeType = 9

    public virtual double AnnualLeaveAccrualDays { get; set; } // TimeType = 10

    public virtual double AnnualLeaveOpeningBal { get; set; } // TimeType = 11

    public virtual double AnnualLeaveClosingBal { get; set; } // TimeType = 12

    /// <summary>
    /// Legacy name: <c>TicketAccrualDaya</c> (typo) -- corrected here, see class remarks. TimeType = 13.
    /// </summary>
    public virtual double TicketAccrualDays { get; set; }

    public virtual double TicketAccrualOpeningBal { get; set; } // TimeType = 14

    public virtual double TicketAccrualClosingBal { get; set; } // TimeType = 15

    public virtual double AirTicketPaidDays { get; set; } // TimeType = 16

    public virtual double GratuityPaidDays { get; set; } // TimeType = 17

    public virtual double ActiveDays { get; set; } // TimeType = 18

    public virtual double AdvanceLeavePaids { get; set; } // TimeType = 19

    public virtual double AdvanceTicketPaids { get; set; } // TimeType = 20

    public virtual double PlannedWorkingMins { get; set; } // TimeType = 21

    public virtual double ActualWorkingMins { get; set; } // TimeType = 22

    public virtual double ActualAbsenceMins { get; set; } // TimeType = 23

    public virtual double NormalDayOTMins { get; set; } // TimeType = 24

    public virtual double RestDayOTMins { get; set; } // TimeType = 25

    public virtual double HolidayOTMins { get; set; } // TimeType = 26

    public virtual double WeekendOTMins { get; set; } // TimeType = 27

    public virtual double PlannedWorkingDays { get; set; } // TimeType = 28

    public virtual double ActualWorkingDays { get; set; } // TimeType = 29

    public virtual double ExtraWorkingDays { get; set; } // TimeType = 30

    public virtual double LieuDays { get; set; } // TimeType = 31

    public virtual double AdvLeaveRecoveryDays { get; set; } // TimeType = 32
}
