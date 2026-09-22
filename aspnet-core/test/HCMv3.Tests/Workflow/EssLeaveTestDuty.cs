using System;
using System.Linq;
using HCMv3.EntityFrameworkCore;
using HCMv3.TimeManagement;

namespace HCMv3.Tests.Workflow;

/// <summary>
/// Gives a test employee a working week. ESS leave counts its days the legacy way -- calendar days less the
/// week-offs, holidays and rest days the type excludes -- and that needs the employee's duty period, exactly as
/// legacy <c>EmployeeAbsence.ActualLeaveDays</c> did. A real employee always has one (Go-Live assigns it);
/// a bare test employee does not, and leave for them fails with "Employee Duty Period not found".
/// </summary>
public static class EssLeaveTestDuty
{
    /// <summary>Every day a working day, so calendar days and leave days agree and date arithmetic in the tests
    /// stays obvious. Tests about weekends replace it with a real week.</summary>
    public static void AssignAllWorkingWeek(HCMv3DbContext context, int tenantId, int employeeId)
    {
        var working = context.Set<Duty>().Add(new Duty
        {
            TenantId = tenantId,
            Code = Guid.NewGuid().ToString("N").Substring(0, 6),
            NameEn = "Work",
            NameAr = "Work",
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0)
        }).Entity;

        context.SaveChanges();

        var period = context.Set<DutyPeriod>().Add(new DutyPeriod
        {
            TenantId = tenantId,
            NameEn = "All week",
            NameAr = "All week",
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2035, 12, 31),
            SundayDutyId = working.Id,
            MondayDutyId = working.Id,
            TuesdayDutyId = working.Id,
            WednesdayDutyId = working.Id,
            ThursdayDutyId = working.Id,
            FridayDutyId = working.Id,
            SaturdayDutyId = working.Id
        }).Entity;

        context.SaveChanges();

        context.Set<EmployeeDutyPeriod>().Add(new EmployeeDutyPeriod
        {
            TenantId = tenantId,
            EmployeeId = employeeId,
            DutyPeriodId = period.Id,
            StartDate = new DateTime(2020, 1, 1),
            EndDate = new DateTime(2035, 12, 31)
        });

        context.SaveChanges();
    }

    /// <summary>Removes the employee's duty periods, so a test can give them a different week.</summary>
    public static void ClearDutyPeriods(HCMv3DbContext context, int employeeId)
    {
        foreach (var row in context.Set<EmployeeDutyPeriod>().Where(p => p.EmployeeId == employeeId).ToList())
        {
            context.Set<EmployeeDutyPeriod>().Remove(row);
        }

        context.SaveChanges();
    }
}
