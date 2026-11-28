using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILeaveUnpaidSplitPlanner"/>
/// <remarks>
/// The paid part ends on the day that makes its COUNTABLE days equal the whole days available -- walking past
/// week-offs and holidays (<see cref="IEmployeeAbsenceCalculationService.CalcLeaveEndDateAsync"/>), which is why
/// it is a date walk rather than "start + balance". The unpaid part runs from the next day to the end asked for.
/// </remarks>
[UnitOfWork]
public class LeaveUnpaidSplitPlanner : HCMv3DomainServiceBase, ILeaveUnpaidSplitPlanner
{
    public const string UnpaidLeaveName = "Unpaid Leave";
    public const string SickLeaveUnpaidName = "Sick Leave Unpaid";
    public const string UnpaidLeaveCode = "2";
    public const string SickLeaveCode = "3";

    private readonly IEmployeeAbsenceCalculationService _calculation;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;

    public LeaveUnpaidSplitPlanner(
        IEmployeeAbsenceCalculationService calculation,
        IRepository<AbsenceType> absenceTypeRepository)
    {
        _calculation = calculation;
        _absenceTypeRepository = absenceTypeRepository;
    }

    public async Task<LeaveUnpaidSplitPlan> PlanAsync(
        int employeeId, int absenceTypeId, DateTime startDate, DateTime endDate, double availableDays,
        int excludeEmployeeAbsenceId = 0)
    {
        var start = startDate.Date;
        var end = endDate.Date;

        var plan = new LeaveUnpaidSplitPlan
        {
            RequestedDays = await _calculation.CalculateTotalLeaveDaysAsync(employeeId, absenceTypeId, start, end, 0)
        };

        if (plan.RequestedDays <= availableDays)
        {
            return Fits(plan, start, end);
        }

        // Whole days only: legacy takes floor(balance), so a fraction of a day is not split across two rows.
        plan.PaidDaysAllowed = Math.Max(0, Math.Floor(availableDays));

        DateTime? paidEnd = null;
        if (plan.PaidDaysAllowed > 0)
        {
            paidEnd = await _calculation.CalcLeaveEndDateAsync(
                employeeId, absenceTypeId, start.AddDays(-1), plan.PaidDaysAllowed, excludeEmployeeAbsenceId);
        }

        if (paidEnd >= end)
        {
            return Fits(plan, start, end);
        }

        plan.PaidEndDate = paidEnd;
        if (paidEnd != null)
        {
            plan.PaidCalendarDays = (paidEnd.Value - start).TotalDays + 1;
            plan.PaidLeaveDays = await _calculation.CalculateTotalLeaveDaysAsync(employeeId, absenceTypeId, start, paidEnd.Value, 0);
        }

        plan.UnpaidStartDate = (paidEnd ?? start.AddDays(-1)).AddDays(1);
        plan.UnpaidEndDate = end;
        plan.UnpaidCalendarDays = (end - plan.UnpaidStartDate.Value).TotalDays + 1;

        // Counted with the MAIN type's week-off and holiday rules, as legacy's SaveAdditionalUnpaidLeave did.
        plan.UnpaidLeaveDays = await _calculation.CalculateTotalLeaveDaysAsync(
            employeeId, absenceTypeId, plan.UnpaidStartDate.Value, end, 0);

        return plan;
    }

    public AbsenceType FindAdditionalUnpaidLeaveType(AbsenceType mainType)
    {
        var isSick = mainType != null && (mainType.Code == SickLeaveCode || mainType.QuotaType == QuotaType.SickLeave);
        var wantedName = isSick ? SickLeaveUnpaidName : UnpaidLeaveName;

        return _absenceTypeRepository.GetAll().Where(t => t.NameEn == wantedName).OrderBy(t => t.Id).FirstOrDefault()
               ?? _absenceTypeRepository.GetAll().Where(t => t.Code == UnpaidLeaveCode).OrderBy(t => t.Id).FirstOrDefault();
    }

    private static LeaveUnpaidSplitPlan Fits(LeaveUnpaidSplitPlan plan, DateTime start, DateTime end)
    {
        plan.FitsWithinBalance = true;
        plan.PaidEndDate = end;
        plan.PaidLeaveDays = plan.RequestedDays;
        plan.PaidCalendarDays = (end - start).TotalDays + 1;
        return plan;
    }
}
