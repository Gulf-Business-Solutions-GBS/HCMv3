using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings;
using HCMv3.SystemSettings.Services;
using HCMv3.TimeManagement;
using HCMv3.TimeManagement.Services;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="IProbationEndDateService"/>
/// <remarks>
/// Ported from legacy <c>Employee.GetProbationEndDate</c> (2159-2261). Legacy walked forward from the joining
/// date counting only the days the employee was actually available, and stopped once it had counted
/// <c>ProbationDays</c> of them; everything skipped pushes the end date out by a day.
///
/// <para>
/// A day does not count when it is unpaid leave, a public holiday, a week-off or rest day, or a day the time
/// engine recorded as absent (approved absence when <c>EnableTimeRegisterApproval</c> is on, as legacy chose
/// between <c>AbsentDay</c> and <c>ApprovedAbsentDay</c>). Paid leave DOES count -- legacy only collected
/// absences whose type is <c>IsPaid == false</c>.
/// </para>
/// <para>
/// Legacy's loop had no bound, so a configuration in which no day ever counts would hang; this one gives up
/// after ten years of calendar and returns the plain calendar-days answer, which is what legacy fell back to
/// when its own calculation threw (it swallowed the exception).
/// </para>
/// </remarks>
[UnitOfWork]
public class ProbationEndDateService : HCMv3DomainServiceBase, IProbationEndDateService
{
    private const int MaxWalkDays = 3650;

    private readonly IRepository<Employee> _employees;
    private readonly IRepository<EmployeeAbsence> _absences;
    private readonly IRepository<AbsenceType> _absenceTypes;
    private readonly IRepository<TimeRegister> _timeRegisters;
    private readonly ISysSettingsAccessor _settings;
    private readonly ITimeDataService _timeDataService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public ProbationEndDateService(
        IRepository<Employee> employees,
        IRepository<EmployeeAbsence> absences,
        IRepository<AbsenceType> absenceTypes,
        IRepository<TimeRegister> timeRegisters,
        ISysSettingsAccessor settings,
        ITimeDataService timeDataService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _employees = employees;
        _absences = absences;
        _absenceTypes = absenceTypes;
        _timeRegisters = timeRegisters;
        _settings = settings;
        _timeDataService = timeDataService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<DateTime> CalculateAsync(int employeeId)
    {
        var employee = _employees.GetAll().FirstOrDefault(e => e.Id == employeeId);
        if (employee == null)
        {
            return default;
        }

        var calendarDaysAnswer = employee.JoiningDate.Date.AddDays(employee.ProbationDays);

        var personnelSettings = await _settings.GetPersonnelSettingsAsync();
        if (personnelSettings?.ProbationDaysCalculation != ProbationDaysCalculation.WorkingDays
            || employee.ProbationDays <= 0)
        {
            return calendarDaysAnswer;
        }

        var unpaidLeaveDays = UnpaidLeaveDates(employeeId);
        var absentDays = await AbsentDatesAsync(employeeId, employee.JoiningDate.Date);

        var joining = employee.JoiningDate.Date;
        var counted = 0;
        var walked = 0;

        while (counted < employee.ProbationDays)
        {
            if (walked > MaxWalkDays)
            {
                return calendarDaysAnswer;
            }

            var day = joining.AddDays(walked);

            var skipped = unpaidLeaveDays.Contains(day)
                          || absentDays.Contains(day)
                          || await _timeDataService.IsHolidayAsync(day, null, employee)
                          || await _timeDataService.IsWeekEndAsync(day, null, employee)
                          || await _timeDataService.IsRestDayAsync(day, null, employee);

            if (!skipped)
            {
                counted++;
            }

            walked++;
        }

        return joining.AddDays(walked);
    }

    public async Task RecalculateAfterLeaveChangeAsync(int employeeId, int absenceTypeId)
    {
        var personnelSettings = await _settings.GetPersonnelSettingsAsync();
        if (personnelSettings?.ProbationDaysCalculation != ProbationDaysCalculation.WorkingDays)
        {
            return;
        }

        // Only unpaid leave moves probation (legacy collected absences with IsPaid == false).
        var isPaid = _absenceTypes.GetAll().Where(t => t.Id == absenceTypeId).Select(t => (bool?)t.IsPaid).FirstOrDefault();
        if (isPaid != false)
        {
            return;
        }

        var employee = _employees.GetAll().FirstOrDefault(e => e.Id == employeeId);
        if (employee == null)
        {
            return;
        }

        // The leave that prompted this may still be pending in the unit of work, and the walk below reads it
        // back by query -- so it has to be in the database first, or the recomputed date would ignore it.
        await _unitOfWorkManager.Current.SaveChangesAsync();

        var probationEnd = await CalculateAsync(employeeId);
        if (probationEnd == employee.ProbationEndDate.Date)
        {
            return;
        }

        employee.ProbationEndDate = probationEnd;
        await _employees.UpdateAsync(employee);
    }

    /// <summary>Every day covered by an unpaid leave of this employee -- the days that do not count.</summary>
    private HashSet<DateTime> UnpaidLeaveDates(int employeeId)
    {
        List<(DateTime Start, DateTime End)> ranges;

        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            ranges = _absences.GetAll()
                .Where(a => a.EmployeeId == employeeId
                            && !a.AbsenceType.IsPaid
                            && (a.WorkflowStatus == null
                                || a.WorkflowStatus == RequestStatus.Approved
                                || a.WorkflowStatus == RequestStatus.Closed))
                .Select(a => new { a.StartDate, a.EndDate })
                .ToList()
                .Select(a => (a.StartDate.Date, a.EndDate.Date))
                .ToList();
        }

        var days = new HashSet<DateTime>();

        foreach (var (start, end) in ranges)
        {
            for (var day = start; day <= end; day = day.AddDays(1))
            {
                days.Add(day);
            }
        }

        return days;
    }

    /// <summary>Days the time engine recorded as absent, which legacy also refused to count.</summary>
    private async Task<HashSet<DateTime>> AbsentDatesAsync(int employeeId, DateTime fromDate)
    {
        var timeSettings = await _settings.GetTimeSettingsAsync();
        var useApproved = timeSettings?.EnableTimeRegisterApproval ?? false;

        var days = _timeRegisters.GetAll()
            .Where(r => r.EmployeeId == employeeId
                        && r.DayDate >= fromDate
                        && (useApproved ? r.ApprovedAbsentDay != 0 : r.AbsentDay != 0))
            .Select(r => r.DayDate)
            .ToList();

        return days.Select(d => d.Date).ToHashSet();
    }
}
