using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.PayrollManagement.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILeavePlanCalculator"/>
/// <remarks>
/// Queries are synchronous over <c>GetAll()</c>, as in <c>EmployeeAbsencePreviewService</c>, so the service can
/// be tested over in-memory repositories.
/// </remarks>
public class LeavePlanCalculator : HCMv3DomainServiceBase, ILeavePlanCalculator
{
    /// <summary>Legacy <c>r.Code == "1"</c> in <c>LeavePlanner.ProjectedLeaveBalance</c> and
    /// <c>BaseRepository.AnnualPaidLeaveTypCode</c>; every other port keeps its own copy of it too.</summary>
    private const string AnnualLeaveCode = "1";

    /// <summary>Legacy <c>BaseRepository.GetProjectedLeaveBalance</c> rounds to three places.</summary>
    private const int BalanceRoundoff = 3;

    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<EmployeeOrgAssignment> _orgAssignmentRepository;
    private readonly IAccrualService _accrualService;
    private readonly IEmployeeAbsenceCalculationService _absenceCalculationService;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LeavePlanCalculator(
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<EmployeeOrgAssignment> orgAssignmentRepository,
        IAccrualService accrualService,
        IEmployeeAbsenceCalculationService absenceCalculationService,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _absenceTypeRepository = absenceTypeRepository;
        _orgAssignmentRepository = orgAssignmentRepository;
        _accrualService = accrualService;
        _absenceCalculationService = absenceCalculationService;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public double GetPlannedDays(DateTime startDate, DateTime endDate)
    {
        if (startDate == default || endDate == default || endDate.Date < startDate.Date)
        {
            return 0;
        }

        return endDate.Date.Subtract(startDate.Date).Days + 1;
    }

    public async Task<double?> GetProjectedLeaveBalanceAsync(int employeeId, DateTime asOnDate)
    {
        if (employeeId <= 0 || asOnDate == default)
        {
            return null;
        }

        var annualLeaveTypeId = AnnualLeaveTypeId();
        if (!annualLeaveTypeId.HasValue)
        {
            return null;
        }

        var date = asOnDate.Date;

        // Legacy AccrualService.GetAsOnDateQuotaWithProjection: the salary profile of the org assignment that
        // covers the as-on date, not the employee's current one -- a plan for next year is projected on the
        // profile they will be on then, if HR has already dated it.
        var subGroupId = _orgAssignmentRepository.GetAll()
            .Where(a => a.EmployeeId == employeeId && a.StartDate <= date && a.EndDate >= date)
            .OrderBy(a => a.Id)
            .Select(a => a.EmployeeSubGroupId)
            .FirstOrDefault();

        double balance;
        using (ApprovedLeaveOnly())
        {
            balance = await _accrualService.GetAsOnDateQuotaWithProjectionAsync(employeeId, subGroupId, annualLeaveTypeId.Value, date);
        }

        return Math.Round(balance, BalanceRoundoff);
    }

    public async Task<double?> GetAnnualLeaveDaysAsync(int employeeId, DateTime startDate, DateTime endDate)
    {
        if (employeeId <= 0 || GetPlannedDays(startDate, endDate) <= 0)
        {
            return null;
        }

        var annualLeaveTypeId = AnnualLeaveTypeId();
        if (!annualLeaveTypeId.HasValue)
        {
            return null;
        }

        using (ApprovedLeaveOnly())
        {
            return await _absenceCalculationService.CalculateTotalLeaveDaysAsync(
                employeeId, annualLeaveTypeId.Value, startDate.Date, endDate.Date, 0);
        }
    }

    public void Validate(DateTime startDate, DateTime endDate)
    {
        if (startDate == default)
        {
            throw new UserFriendlyException(L("StartDateIsRequired"));
        }

        if (endDate == default)
        {
            throw new UserFriendlyException(L("EndDateIsRequired"));
        }

        if (endDate.Date < startDate.Date)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    public async Task RefreshFiguresAsync(LeavePlan plan)
    {
        if (plan == null)
        {
            throw new ArgumentNullException(nameof(plan));
        }

        plan.PlannedDays = GetPlannedDays(plan.StartDate, plan.EndDate);
        plan.ProjectedLeaveBalance = await GetProjectedLeaveBalanceAsync(plan.EmployeeId, plan.StartDate);
    }

    /// <summary>
    /// Switches the <c>ApprovedOnly</c> filter ON for the accrual read, whatever the caller had it set to.
    ///
    /// <para>
    /// The self-service base and the workflow engine both run with it OFF while they handle a request (they
    /// have to, to see the request's own row), and a projection taken in that state would count the employee's
    /// PENDING leave as used. Pending leave reserves no quota; and legacy's projection read
    /// <c>EmployeeAbsence</c>, which in legacy held approved leave only, the requests living in their own table.
    /// So the filter is forced on here rather than trusted.
    /// </para>
    /// </summary>
    private IDisposable ApprovedLeaveOnly()
    {
        return _unitOfWorkManager?.Current?.EnableFilter(HCMv3DataFilters.ApprovedOnly) ?? NoScope.Instance;
    }

    private sealed class NoScope : IDisposable
    {
        public static readonly NoScope Instance = new NoScope();

        public void Dispose()
        {
        }
    }

    private int? AnnualLeaveTypeId()
    {
        return _absenceTypeRepository.GetAll()
            .Where(t => t.Code == AnnualLeaveCode)
            .OrderBy(t => t.Id)
            .Select(t => (int?)t.Id)
            .FirstOrDefault();
    }
}
