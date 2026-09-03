using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Timing;
using HCMv3.Ess.Dto;
using HCMv3.PersonnelAdministration;
using HCMv3.PersonnelAdministration.Services;
using HCMv3.TimeManagement;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Ess;

/// <summary>
/// Everything the main dashboard shows about the signed-in person, in one call.
///
/// ONE CALL, NOT SIX. The dashboard draws an identity card, leave balances, today's attendance and the
/// people-news counters at once; six endpoints would be six round trips before anything appears, and
/// four of them would each have to resolve "who am I" again.
///
/// NO EMPLOYEE RECORD IS A NORMAL STATE, not an error. A tenant administrator is a user without an
/// Employee row, and they still open the dashboard. Profile comes back null and the personal tiles are
/// simply absent, rather than the whole dashboard failing.
/// </summary>
[AbpAuthorize]
public class MyDashboardAppService : HCMv3AppServiceBase, IMyDashboardAppService
{
    private readonly IEmployeeSessionService _employeeSession;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly ILeaveBalanceService _leaveBalanceService;

    public MyDashboardAppService(
        IEmployeeSessionService employeeSession,
        IRepository<Employee> employeeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        ILeaveBalanceService leaveBalanceService)
    {
        _employeeSession = employeeSession;
        _employeeRepository = employeeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _leaveBalanceService = leaveBalanceService;
    }

    public async Task<MyDashboardSummaryDto> GetSummary()
    {
        var today = Clock.Now.Date;
        var employeeId = await _employeeSession.GetCurrentEmployeeIdAsync();

        var summary = new MyDashboardSummaryDto
        {
            LeaveBalances = new List<MyDashboardLeaveBalanceDto>(),
            // Alerts are about colleagues, not about you, so they are filled in even for a user with no
            // employee record of their own.
            Alerts = await GetAlertsAsync(today),
        };

        if (!employeeId.HasValue)
        {
            return summary;
        }

        summary.Profile = await GetProfileAsync(employeeId.Value, today);
        summary.LeaveBalances = await GetLeaveBalancesAsync(employeeId.Value);

        return summary;
    }

    private async Task<MyDashboardProfileDto> GetProfileAsync(int employeeId, DateTime today)
    {
        var employee = await _employeeRepository.GetAll()
            .Where(e => e.Id == employeeId)
            .FirstOrDefaultAsync();

        if (employee == null)
        {
            return null;
        }

        // The assignment in force TODAY. Ordering by StartDate descending and taking the first is what
        // makes a future-dated transfer not silently become the current placement.
        var assignment = await _orgAssignmentRepository.GetAll()
            .Include(a => a.JobTitle)
            .Include(a => a.OrganizationUnit)
            .Include(a => a.WorkLocation)
            .Include(a => a.CostCenter)
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= today)
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync();

        return new MyDashboardProfileDto
        {
            EmployeeId = employee.Id,
            Code = employee.Code,
            FullName = string.Join(' ', new[] { employee.FirstNameEn, employee.LastNameEn }.Where(p => !string.IsNullOrWhiteSpace(p))),

            // Every one of these is null-tolerant on purpose. The assignment may not exist, and on this
            // deployment OrganizationUnitId is null on every row, so Department renders blank -- which is
            // the honest display of an org chart that has not been populated.
            JobTitle = assignment?.JobTitle?.NameEn,
            Department = assignment?.OrganizationUnit?.DisplayName,
            WorkLocation = assignment?.WorkLocation?.NameEn,
            CostCenter = assignment?.CostCenter?.NameEn,
        };
    }

    /// <summary>
    /// Leave balances from ILeaveBalanceService -- the same calculation the ESS leave screen uses.
    ///
    /// NOT recomputed here. Balance is Accrued + Adjusted - Forfeited - Used, and Bookable subtracts
    /// what is already pending approval. Re-deriving that from the quota rows would give the dashboard
    /// its own opinion of how much leave somebody has, and the day the two disagreed nobody would know
    /// which to believe.
    ///
    /// BOOKABLE, not Remaining, is what the tile shows: the number a person acts on is what they can
    /// still apply for, and counting days already awaiting approval as available invites a second
    /// request that cannot be granted.
    /// </summary>
    private async Task<List<MyDashboardLeaveBalanceDto>> GetLeaveBalancesAsync(int employeeId)
    {
        var balances = await _leaveBalanceService.GetBalancesAsync(employeeId);

        return balances
            .Where(b => !string.IsNullOrWhiteSpace(b.AbsenceTypeNameEn))
            .Select(b => new MyDashboardLeaveBalanceDto
            {
                LeaveType = b.AbsenceTypeNameEn,
                Balance = (decimal)b.Bookable,
            })
            .OrderByDescending(b => b.Balance)
            .ToList();
    }

    /// <summary>
    /// Birthdays and work anniversaries falling in the current month, across the tenant.
    ///
    /// Counts only. The names are deliberately not returned: a counter does not need every colleague's
    /// date of birth to be readable by anyone who opens the dashboard.
    /// </summary>
    private async Task<MyDashboardAlertsDto> GetAlertsAsync(DateTime today)
    {
        var month = today.Month;

        var birthdays = await _employeeRepository.GetAll()
            .CountAsync(e => e.BirthDate.Month == month);

        var anniversaries = await _employeeRepository.GetAll()
            // Excludes people who joined THIS month in the current year: their first anniversary has not
            // happened, and counting a new starter as an anniversary is simply wrong.
            .CountAsync(e => e.JoiningDate.Month == month && e.JoiningDate.Year < today.Year);

        return new MyDashboardAlertsDto
        {
            BirthdaysThisMonth = birthdays,
            WorkAnniversariesThisMonth = anniversaries,
        };
    }
}
