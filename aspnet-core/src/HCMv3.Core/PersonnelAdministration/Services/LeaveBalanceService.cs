using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;
using HCMv3.PayrollManagement;
using HCMv3.Workflow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILeaveBalanceService"/>
/// <remarks>
/// <c>HCMv3.Core</c> has no reference to EntityFrameworkCore, so this uses the synchronous
/// <c>IQueryable</c> from <c>IRepository&lt;T&gt;.GetAll()</c> -- the same pattern as <c>RlsService</c> and
/// <c>RequestMasterResolver</c>.
/// </remarks>
[UnitOfWork]
public class LeaveBalanceService : HCMv3DomainServiceBase, ILeaveBalanceService
{
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<EmployeeAbsence> _absenceRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IUnitOfWorkManager _unitOfWorkManager;

    public LeaveBalanceService(
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<EmployeeAbsence> absenceRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IUnitOfWorkManager unitOfWorkManager)
    {
        _quotaRepository = quotaRepository;
        _absenceRepository = absenceRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _unitOfWorkManager = unitOfWorkManager;
    }

    public async Task<LeaveBalance> GetBalanceAsync(int employeeId, int absenceTypeId, DateTime? asOf = null)
    {
        var balances = await GetBalancesAsync(employeeId, asOf);

        return balances.FirstOrDefault(b => b.AbsenceTypeId == absenceTypeId)
               ?? new LeaveBalance { AbsenceTypeId = absenceTypeId };
    }

    public Task<List<LeaveBalance>> GetBalancesAsync(int employeeId, DateTime? asOf = null)
    {
        var effectiveDate = (asOf ?? DateTime.Today).Date;

        // The quota row whose entitlement period covers the date. An employee can hold several per type
        // across years, so the period must be selected rather than summed.
        var quotas = _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId
                        && q.EntitlementFrom <= effectiveDate
                        && q.EntitlementTo >= effectiveDate)
            .Select(q => new
            {
                q.AbsenceTypeId,
                q.QuotaAccrued,
                q.QuotaAdjusted,
                q.QuotaForfeited,
                q.QuotaUsed
            })
            .ToList();

        if (quotas.Count == 0)
        {
            return Task.FromResult(new List<LeaveBalance>());
        }

        var pendingByType = GetPendingDaysByAbsenceType(employeeId);

        var absenceTypeIds = quotas.Select(q => q.AbsenceTypeId).Distinct().ToList();
        var absenceTypes = _absenceTypeRepository.GetAll()
            .Where(t => absenceTypeIds.Contains(t.Id))
            .Select(t => new { t.Id, t.NameEn, t.NameAr })
            .ToList()
            .ToDictionary(t => t.Id);

        var balances = quotas
            .GroupBy(q => q.AbsenceTypeId)
            .Select(g =>
            {
                pendingByType.TryGetValue(g.Key, out var pending);
                absenceTypes.TryGetValue(g.Key, out var absenceType);

                return new LeaveBalance
                {
                    AbsenceTypeId = g.Key,
                    AbsenceTypeNameEn = absenceType?.NameEn,
                    AbsenceTypeNameAr = absenceType?.NameAr,
                    Accrued = g.Sum(q => q.QuotaAccrued),
                    Adjusted = g.Sum(q => q.QuotaAdjusted),
                    Forfeited = g.Sum(q => q.QuotaForfeited),
                    Used = g.Sum(q => q.QuotaUsed),
                    Pending = pending
                };
            })
            .OrderBy(b => b.AbsenceTypeNameEn)
            .ToList();

        return Task.FromResult(balances);
    }

    /// <summary>
    /// Days on leave that is submitted but not yet decided, per absence type.
    ///
    /// <para>
    /// The <c>ApprovedOnly</c> filter is disabled here, and this is the one place in the balance calculation
    /// that needs it: pending leave is exactly what that filter hides. Without the opt-out this method would
    /// return nothing and an employee could book the same days repeatedly, each application passing its own
    /// check.
    /// </para>
    /// <para>
    /// Only <see cref="RequestStatus.Open"/> counts. Draft is not yet a claim on anything -- the employee
    /// may never submit it -- and Rejected and Draft-after-send-back have released whatever they held.
    /// Approved days are already in <c>QuotaUsed</c> and would be double-counted here.
    /// </para>
    /// </summary>
    private Dictionary<int, double> GetPendingDaysByAbsenceType(int employeeId)
    {
        using (_unitOfWorkManager.Current.DisableFilter(HCMv3DataFilters.ApprovedOnly))
        {
            return _absenceRepository.GetAll()
                .Where(a => a.EmployeeId == employeeId && a.WorkflowStatus == RequestStatus.Open)
                .GroupBy(a => a.AbsenceTypeId)
                .Select(g => new { AbsenceTypeId = g.Key, Days = g.Sum(a => a.TotalLeaveDays) })
                .ToList()
                .ToDictionary(x => x.AbsenceTypeId, x => x.Days);
        }
    }
}
