using System;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.UI;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Ported verbatim from legacy <c>EmployeeGratuity.cs</c> (<c>PerformValidations</c>, <c>UpdateQuotaBalance</c>,
/// <c>OnDeleting</c>). Rules kept on purpose (payroll-parity), each pinned by a test:
/// <list type="bullet">
/// <item><description>The quota moved is the employee's <see cref="EmployeeGratuityQuota"/> whose DEDUCTION period
/// (<c>DeductionFrom..DeductionTo</c>) covers the paid date.</description></item>
/// <item><description>Editing or deleting a record always looks the previous quota up, and refuses when there is
/// none -- even when the previous days were zero (legacy checks before looking at the days).</description></item>
/// <item><description>Zero paid days charge nothing and need no quota.</description></item>
/// <item><description>The charge is refused when the days are more than the quota balance
/// (<c>Round(QuotaAccrued - (QuotaUsed + QuotaAdjusted), 3)</c>), seen after the give-back when the edit stays on
/// the same quota row. Negative days always pass and add days back.</description></item>
/// </list>
/// </summary>
public class EmployeeGratuityQuotaService : HCMv3DomainServiceBase, IEmployeeGratuityQuotaService
{
    private readonly IRepository<EmployeeGratuityQuota> _quotaRepository;
    private readonly IRepository<Employee> _employeeRepository;

    public EmployeeGratuityQuotaService(
        IRepository<EmployeeGratuityQuota> quotaRepository,
        IRepository<Employee> employeeRepository)
    {
        _quotaRepository = quotaRepository;
        _employeeRepository = employeeRepository;
    }

    public async Task ApplyAsync(EmployeeGratuity gratuity, EmployeeGratuitySnapshot previous)
    {
        // << PerformValidations >>
        if (gratuity.EmployeeId == 0)
        {
            throw new UserFriendlyException(L("EmployeeCannotBeBlank"));
        }

        if (gratuity.GratuityPaidDate == default)
        {
            throw new UserFriendlyException(L("IndemnityPaidDateCannotBeNull"));
        }

        // << Update Previous Values >>: only for an existing row (legacy Oid != -1).
        EmployeeGratuityQuota previousQuota = null;
        if (previous != null)
        {
            previousQuota = await CreditPreviousAsync(previous);
            await _quotaRepository.UpdateAsync(previousQuota);
        }

        // << Update New Values >>
        if (gratuity.GratuityPaidDays == 0)
        {
            return;
        }

        var quota = FindQuota(gratuity.EmployeeId, gratuity.GratuityPaidDate);
        if (quota == null)
        {
            throw new UserFriendlyException(L("IndemnityQuotaNotFoundForEmployee", await CodeWithFullNameEnAsync(gratuity.EmployeeId)));
        }

        // Same row as the one just credited -> the check below sees the credited balance.
        if (previousQuota != null && quota.Id == previousQuota.Id)
        {
            quota = previousQuota;
        }

        var balance = QuotaBalance(quota);
        if (gratuity.GratuityPaidDays > balance)
        {
            throw new UserFriendlyException(L("IndemnityQuotaNotAvailable", balance, await CodeWithFullNameEnAsync(gratuity.EmployeeId)));
        }

        quota.QuotaUsed += gratuity.GratuityPaidDays;
        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task ReverseAsync(EmployeeGratuity gratuity)
    {
        var quota = await CreditPreviousAsync(EmployeeGratuitySnapshot.Of(gratuity));
        await _quotaRepository.UpdateAsync(quota);
    }

    /// <summary>
    /// The give-back shared by save and delete: takes the previous days off the quota covering the previous paid
    /// date. Legacy refuses when there is no such quota, whatever the days were.
    /// </summary>
    private async Task<EmployeeGratuityQuota> CreditPreviousAsync(EmployeeGratuitySnapshot previous)
    {
        var quota = FindQuota(previous.EmployeeId, previous.GratuityPaidDate);
        if (quota == null)
        {
            throw new UserFriendlyException(L("IndemnityQuotaNotFoundForEmployee", await CodeWithFullNameEnAsync(previous.EmployeeId)));
        }

        quota.QuotaUsed -= previous.GratuityPaidDays;
        return quota;
    }

    private EmployeeGratuityQuota FindQuota(int employeeId, DateTime paidDate)
    {
        var date = paidDate.Date;

        return _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId && q.DeductionFrom <= date && q.DeductionTo >= date)
            .OrderBy(q => q.Id)
            .FirstOrDefault();
    }

    /// <summary>Legacy <c>EmployeeGratuityQuota.QuotaBalance</c>, rounded to 3.</summary>
    public static double QuotaBalance(EmployeeGratuityQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted), 3);

    /// <summary>Legacy XPO computed <c>Employee.CodeWithFullNameEn</c>.</summary>
    private async Task<string> CodeWithFullNameEnAsync(int employeeId)
    {
        var employee = await _employeeRepository.FirstOrDefaultAsync(employeeId);
        return string.IsNullOrWhiteSpace(employee?.FullNameEn) ? employee?.Code : employee.Code + " " + employee.FullNameEn;
    }
}
