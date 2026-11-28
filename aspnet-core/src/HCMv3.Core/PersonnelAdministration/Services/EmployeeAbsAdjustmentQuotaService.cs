using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Abp.UI;
using HCMv3.PayrollManagement;
using HCMv3.SystemSettings.Services;

namespace HCMv3.PersonnelAdministration.Services;

/// <summary>
/// Ported verbatim from legacy <c>EmployeeAbsAdjustment.cs</c> (<c>UpdateQuotaBalance</c>, <c>OnDeleting</c>).
/// Rules kept on purpose (payroll-parity), each pinned by a test:
/// <list type="bullet">
/// <item><description>Only an annual type (<c>Code == "1"</c> or <see cref="QuotaType.AnnualLeave"/>) moves a
/// quota, and the quota moved is always the one of the type with <c>Code == "1"</c> whose entitlement period
/// covers the effective date.</description></item>
/// <item><description><see cref="AdjustmentType.NoImpactOnQuota"/> never moves a quota: the give-back looks at the
/// PREVIOUS adjustment type, the charge at the NEW one.</description></item>
/// <item><description>Dates before <c>ManageTimeSettings.EarliestEvaluationDate</c> do not move the quota.</description></item>
/// <item><description>A zero adjustment gives back the previous value but charges nothing.</description></item>
/// <item><description>The charge is refused only when it is greater than the balance plus, when the Code "1"
/// type has <c>EnableNegativeBalance</c>, its <c>NegativeBalanceLimit</c>. A negative adjustment always passes
/// and adds days back.</description></item>
/// </list>
/// </summary>
public class EmployeeAbsAdjustmentQuotaService : HCMv3DomainServiceBase, IEmployeeAbsAdjustmentQuotaService
{
    /// <summary>Legacy's <c>AbsenceType.Code == "1"</c> for annual leave.</summary>
    private const string AnnualLeaveAbsenceTypeCode = "1";

    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ISysSettingsAccessor _settings;

    public EmployeeAbsAdjustmentQuotaService(
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<Employee> employeeRepository,
        ISysSettingsAccessor settings)
    {
        _quotaRepository = quotaRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeRepository = employeeRepository;
        _settings = settings;
    }

    public async Task ApplyAsync(EmployeeAbsAdjustment adjustment, EmployeeAbsAdjustmentSnapshot previous)
    {
        var earliestEvaluationDate = await GetEarliestEvaluationDateAsync();

        // << Update Previous Values >>: only for an existing row (legacy Oid != -1).
        EmployeeAbsenceQuota previousQuota = null;
        if (previous != null)
        {
            previousQuota = await CreditPreviousAsync(previous, earliestEvaluationDate);
            if (previousQuota != null)
            {
                await _quotaRepository.UpdateAsync(previousQuota);
            }
        }

        // << Update New Values >>
        if (adjustment.QuotaAdjusted == 0 || adjustment.AdjustmentType == AdjustmentType.NoImpactOnQuota)
        {
            return;
        }

        var absenceType = await _absenceTypeRepository.FirstOrDefaultAsync(adjustment.AbsenceTypeId);
        if (!IsAnnual(absenceType) || adjustment.EffectiveDate.Date < earliestEvaluationDate)
        {
            return;
        }

        var annualType = await GetAnnualTypeAsync();
        var quota = annualType == null ? null : FindQuota(adjustment.EmployeeId, annualType.Id, adjustment.EffectiveDate);
        if (quota == null)
        {
            // Legacy names the row's own type here.
            var employee = await _employeeRepository.FirstOrDefaultAsync(adjustment.EmployeeId);
            throw new UserFriendlyException(L("NoLeaveQuotaFoundForEmployeeAndAbsenceType",
                CodeWithFullNameEn(employee), absenceType.NameEn));
        }

        // Same row as the one just credited -> the check below sees the credited balance.
        if (previousQuota != null && quota.Id == previousQuota.Id)
        {
            quota = previousQuota;
        }

        var actualBalance = QuotaBalance(quota);
        var balance = actualBalance;
        if (annualType.EnableNegativeBalance)
        {
            balance += annualType.NegativeBalanceLimit;
        }

        if (adjustment.QuotaAdjusted > balance)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(adjustment.EmployeeId);
            var lines = new List<string>
            {
                L("AbsAdjustmentEmployeeLine", CodeWithFullNameEn(employee)),
                L("AbsAdjustmentAbsenceTypeLine", absenceType.NameEn),
                L("AbsAdjustmentAvailableQuotaLine", actualBalance)
            };

            // Legacy reads these two lines off the row's own type, not the Code "1" type.
            if (absenceType.EnableNegativeBalance && absenceType.NegativeBalanceLimit != 0)
            {
                lines.Add(L("AbsAdjustmentNegativeBalanceLimitLine", absenceType.NegativeBalanceLimit));
                lines.Add(L("AbsAdjustmentRemainingQuotaLine", balance));
            }

            lines.Add(L("AbsAdjustmentAppliedQuotaLine", adjustment.QuotaAdjusted));

            throw new UserFriendlyException(L("AbsenceQuotaNotAvailable"), string.Join(Environment.NewLine, lines));
        }

        quota.QuotaAdjusted = Math.Round(quota.QuotaAdjusted + adjustment.QuotaAdjusted, 6);
        await _quotaRepository.UpdateAsync(quota);
    }

    public async Task ReverseAsync(EmployeeAbsAdjustment adjustment)
    {
        var earliestEvaluationDate = await GetEarliestEvaluationDateAsync();
        var quota = await CreditPreviousAsync(EmployeeAbsAdjustmentSnapshot.Of(adjustment), earliestEvaluationDate);
        if (quota != null)
        {
            await _quotaRepository.UpdateAsync(quota);
        }
    }

    /// <summary>
    /// The give-back shared by save and delete: takes the previous value off the Code "1" quota covering the
    /// previous date, unfloored. Returns the row credited, or <c>null</c> when nothing was credited.
    /// </summary>
    private async Task<EmployeeAbsenceQuota> CreditPreviousAsync(EmployeeAbsAdjustmentSnapshot previous, DateTime earliestEvaluationDate)
    {
        if (previous.AdjustmentType == AdjustmentType.NoImpactOnQuota || previous.EffectiveDate.Date < earliestEvaluationDate)
        {
            return null;
        }

        var previousType = await _absenceTypeRepository.FirstOrDefaultAsync(previous.AbsenceTypeId);
        if (!IsAnnual(previousType))
        {
            return null;
        }

        // From here on legacy's "previous type" IS the Code "1" type -- the message below names it.
        var annualType = await GetAnnualTypeAsync();
        var quota = annualType == null ? null : FindQuota(previous.EmployeeId, annualType.Id, previous.EffectiveDate);
        if (quota == null)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(previous.EmployeeId);
            throw new UserFriendlyException(L("NoLeaveQuotaFoundForEmployeeAndAbsenceType",
                CodeWithFullNameEn(employee), (annualType ?? previousType).NameEn));
        }

        quota.QuotaAdjusted = Math.Round(quota.QuotaAdjusted - previous.QuotaAdjusted, 6);
        return quota;
    }

    private EmployeeAbsenceQuota FindQuota(int employeeId, int absenceTypeId, DateTime effectiveDate)
    {
        var date = effectiveDate.Date;

        return _quotaRepository.GetAll()
            .Where(q => q.EmployeeId == employeeId
                        && q.AbsenceTypeId == absenceTypeId
                        && q.EntitlementFrom <= date
                        && q.EntitlementTo >= date)
            .OrderBy(q => q.Id)
            .FirstOrDefault();
    }

    private Task<AbsenceType> GetAnnualTypeAsync()
        => _absenceTypeRepository.FirstOrDefaultAsync(t => t.Code == AnnualLeaveAbsenceTypeCode);

    private async Task<DateTime> GetEarliestEvaluationDateAsync()
    {
        var timeSettings = await _settings.GetTimeSettingsAsync();
        return timeSettings?.EarliestEvaluationDate.Date ?? default;
    }

    private static bool IsAnnual(AbsenceType type)
        => type != null && (type.Code == AnnualLeaveAbsenceTypeCode || type.QuotaType == QuotaType.AnnualLeave);

    /// <summary>Legacy <c>EmployeeAbsenceQuota.QuotaBalance</c>, rounded to 2.</summary>
    private static double QuotaBalance(EmployeeAbsenceQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted + q.QuotaForfeited), 2);

    /// <summary>Legacy XPO computed <c>Employee.CodeWithFullNameEn</c>.</summary>
    private static string CodeWithFullNameEn(Employee employee)
        => string.IsNullOrWhiteSpace(employee?.FullNameEn) ? employee?.Code : employee.Code + " " + employee.FullNameEn;
}
