using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.Domain.Uow;

namespace HCMv3.PersonnelAdministration.Services;

/// <inheritdoc cref="ILeaveQuotaLedger"/>
/// <remarks>
/// Ported from legacy <c>EmployeeAbsence.UpdateQuotaInformation</c> (the release-then-charge part, lines
/// 2141-2238) and <c>OnDeleting</c>'s refund (1321-1419).
///
/// <para>
/// The quota row is the one whose ENTITLEMENT period covers the leave's start date -- the same row
/// <see cref="ILeaveBalanceService"/> reads, so the balance an employee is shown and the balance that is charged
/// cannot disagree about which year they mean.
/// </para>
/// </remarks>
[UnitOfWork]
public class LeaveQuotaLedger : HCMv3DomainServiceBase, ILeaveQuotaLedger
{
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;

    public LeaveQuotaLedger(IRepository<EmployeeAbsenceQuota> quotaRepository)
    {
        _quotaRepository = quotaRepository;
    }

    public async Task ConsumeAsync(int employeeId, int absenceTypeId, double days, DateTime asOf)
    {
        if (days <= 0)
        {
            return;
        }

        var quota = Find(employeeId, absenceTypeId, asOf);
        if (quota == null)
        {
            return;
        }

        quota.QuotaUsed = Math.Round(quota.QuotaUsed + days, 6);

        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task ReleaseAsync(int employeeId, int absenceTypeId, double days, DateTime asOf)
    {
        if (days <= 0)
        {
            return;
        }

        var quota = Find(employeeId, absenceTypeId, asOf);
        if (quota == null)
        {
            return;
        }

        // Floored at zero: a negative Used would read as entitlement the employee never had, and if the stored
        // figure has drifted the safe direction to fail is "no free days".
        quota.QuotaUsed = Math.Round(Math.Max(0, quota.QuotaUsed - days), 6);

        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task MoveAsync(
        int employeeId,
        int previousAbsenceTypeId,
        double previousDays,
        DateTime previousAsOf,
        int newAbsenceTypeId,
        double newDays,
        DateTime newAsOf)
    {
        await ReleaseAsync(employeeId, previousAbsenceTypeId, previousDays, previousAsOf);
        await ConsumeAsync(employeeId, newAbsenceTypeId, newDays, newAsOf);
    }

    private EmployeeAbsenceQuota Find(int employeeId, int absenceTypeId, DateTime asOf)
    {
        var effectiveDate = asOf.Date;

        return _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId
                        && q.AbsenceTypeId == absenceTypeId
                        && q.EntitlementFrom <= effectiveDate
                        && q.EntitlementTo >= effectiveDate)
            .OrderBy(q => q.EntitlementFrom)
            .FirstOrDefault();
    }
}
