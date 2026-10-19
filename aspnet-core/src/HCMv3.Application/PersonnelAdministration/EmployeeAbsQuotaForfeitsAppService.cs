using System;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement;
using HCMv3.PersonnelAdministration.Dto;
using HCMv3.SystemSettings.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeAbsQuotaForfeit"/> (ported from legacy XAF
/// EmployeeAbsQuotaForfeit business object, Wave 1 deferred bucket). Records the manual forfeiture
/// transaction AND, as legacy did, moves <c>QuotaForfeited</c> on the linked annual-leave
/// <see cref="EmployeeAbsenceQuota"/> row.
///
/// <para>
/// Ported verbatim from legacy <c>EmployeeAbsQuotaForfeit.cs</c>: <c>OnSaving</c> (lines 117-152, the
/// <c>DontUpdateQuotaBalance</c> gate at 141), <c>UpdateQuotaBalance</c> (162-231) and <c>OnDeleting</c>
/// (232-277). Legacy oddities kept on purpose (payroll-parity rule), each pinned by a test:
/// </para>
/// <list type="bullet">
/// <item><description>Only an annual type (<c>Code == "1"</c> or <see cref="QuotaType.AnnualLeave"/>) moves a
/// quota, and the quota moved is always the one of the type with <c>Code == "1"</c>, even when the forfeit
/// names a different annual-group type (lines 175-182, 200-210).</description></item>
/// <item><description>Dates before <c>ManageTimeSettings.EarliestEvaluationDate</c> do not move the quota
/// (lines 170, 203, 244).</description></item>
/// <item><description>The previous value is given back WITHOUT a floor (lines 189, 262), and the new value is
/// refused only when it is greater than the balance (line 219) -- a negative forfeit is accepted and adds days
/// back.</description></item>
/// <item><description>A zero forfeit releases the previous value but charges nothing (line 198).</description></item>
/// <item><description><c>DontUpdateQuotaBalance</c> is a non-persistent checkbox (lines 60-63): it exists only
/// on the save request. A delete carries no such flag, so a delete always gives the days back (line 236, where
/// the flag is always its default <c>false</c> on a freshly loaded row).</description></item>
/// </list>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits)]
public class EmployeeAbsQuotaForfeitsAppService : HCMv3AppServiceBase, IEmployeeAbsQuotaForfeitsAppService
{
    /// <summary>Legacy's <c>AbsenceType.Code == "1"</c> for annual leave (lines 175, 177, 200, 205).</summary>
    private const string AnnualLeaveAbsenceTypeCode = "1";

    private readonly IRepository<EmployeeAbsQuotaForfeit> _repository;
    private readonly IRepository<EmployeeAbsenceQuota> _quotaRepository;
    private readonly IRepository<AbsenceType> _absenceTypeRepository;
    private readonly IRepository<Employee> _employeeRepository;
    private readonly ISysSettingsAccessor _settings;

    public EmployeeAbsQuotaForfeitsAppService(
        IRepository<EmployeeAbsQuotaForfeit> repository,
        IRepository<EmployeeAbsenceQuota> quotaRepository,
        IRepository<AbsenceType> absenceTypeRepository,
        IRepository<Employee> employeeRepository,
        ISysSettingsAccessor settings)
    {
        _repository = repository;
        _quotaRepository = quotaRepository;
        _absenceTypeRepository = absenceTypeRepository;
        _employeeRepository = employeeRepository;
        _settings = settings;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeAbsQuotaForfeitDto>> GetAll(GetAllEmployeeAbsQuotaForfeitsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(f => f.Employee)
            .Include(f => f.AbsenceType)
            .WhereIf(input.EmployeeId.HasValue, f => f.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeAbsQuotaForfeitDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeAbsQuotaForfeitForEditOutput> GetEmployeeAbsQuotaForfeitForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeAbsQuotaForfeitForEditOutput
        {
            EmployeeAbsQuotaForfeit = new CreateOrEditEmployeeAbsQuotaForfeitDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                AbsenceTypeId = entity.AbsenceTypeId,
                EffectiveDate = entity.EffectiveDate,
                QuotaForfeited = entity.QuotaForfeited,
                Comments = entity.Comments
                // DontUpdateQuotaBalance is non-persistent (legacy line 61): always opens unticked.
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeAbsQuotaForfeitDto input)
    {
        if (input.Id.HasValue)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeAbsQuotaForfeitDto input)
    {
        var entity = MapToEntity(input, new EmployeeAbsQuotaForfeit());
        entity.TenantId = AbpSession.TenantId ?? 0;

        // Legacy line 141: unticked -> move the quota. A new row has no previous values (legacy Oid == -1,
        // line 167), so only the new value is charged.
        if (!input.DontUpdateQuotaBalance)
        {
            await UpdateQuotaBalanceAsync(entity, previous: null);
        }

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeAbsQuotaForfeitDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        // Legacy OnLoaded (lines 108-116): the values as stored, captured before the row is overwritten.
        var previous = PreviousValues.Of(entity);

        MapToEntity(input, entity);

        if (!input.DontUpdateQuotaBalance)
        {
            await UpdateQuotaBalanceAsync(entity, previous);
        }

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeAbsQuotaForfeits_Delete)]
    public async Task Delete(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        // Legacy OnDeleting (lines 232-277): the stored forfeit goes back to the quota. DontUpdateQuotaBalance
        // is non-persistent, so on a row loaded for deletion it is always false (line 236).
        var earliestEvaluationDate = await GetEarliestEvaluationDateAsync();
        var previousQuota = await CreditPreviousQuotaAsync(PreviousValues.Of(entity), earliestEvaluationDate);
        if (previousQuota != null)
        {
            await _quotaRepository.UpdateAsync(previousQuota);
        }

        await _repository.DeleteAsync(entity);
    }

    /// <summary>
    /// Legacy <c>UpdateQuotaBalance</c> (lines 162-231): give back the previous value, then charge the new one.
    /// </summary>
    private async Task UpdateQuotaBalanceAsync(EmployeeAbsQuotaForfeit entity, PreviousValues previous)
    {
        // Legacy line 165.
        var earliestEvaluationDate = await GetEarliestEvaluationDateAsync();

        // << Update Previous Values >> (lines 164-195). Only for an existing row (line 167).
        EmployeeAbsenceQuota previousQuota = null;
        if (previous != null)
        {
            previousQuota = await CreditPreviousQuotaAsync(previous, earliestEvaluationDate);
            if (previousQuota != null)
            {
                await _quotaRepository.UpdateAsync(previousQuota);
            }
        }

        // << Update New Values >> (lines 197-230). Line 198: a zero forfeit charges nothing.
        if (entity.QuotaForfeited == 0)
        {
            return;
        }

        // Line 200.
        var absenceType = await _absenceTypeRepository.FirstOrDefaultAsync(entity.AbsenceTypeId);
        if (!IsAnnual(absenceType))
        {
            return;
        }

        // Line 203.
        if (entity.EffectiveDate < earliestEvaluationDate)
        {
            return;
        }

        // Lines 205-210: always the Code "1" type's quota, whatever annual-group type the row names.
        var annualType = await _absenceTypeRepository.FirstOrDefaultAsync(t => t.Code == AnnualLeaveAbsenceTypeCode);
        var newQuota = annualType == null ? null : FindQuota(entity.EmployeeId, annualType.Id, entity.EffectiveDate);
        if (newQuota == null)
        {
            // Line 212: this message names the row's own type, not the Code "1" type.
            var employee = await _employeeRepository.FirstOrDefaultAsync(entity.EmployeeId);
            throw new UserFriendlyException(L("NoLeaveQuotaFoundForEmployeeAndAbsenceType",
                CodeWithFullNameEn(employee), absenceType.NameEn));
        }

        // Lines 215-218: the same row as the one just credited -> the check below sees the credited balance.
        if (previousQuota != null && newQuota.Id == previousQuota.Id)
        {
            newQuota = previousQuota;
        }

        // Line 219: only "more than the balance" is refused -- a negative forfeit passes and adds days back.
        var balance = QuotaBalance(newQuota);
        if (entity.QuotaForfeited > balance)
        {
            // Line 220: this message uses the bare employee Code.
            var employee = await _employeeRepository.FirstOrDefaultAsync(entity.EmployeeId);
            throw new UserFriendlyException(L("AbsenceQuotaNotAvailableOnlyXDaysAvailable",
                balance, employee?.Code, absenceType.NameEn));
        }

        // Line 223.
        newQuota.QuotaForfeited = Math.Round(newQuota.QuotaForfeited + entity.QuotaForfeited, 6);
        await _quotaRepository.UpdateAsync(newQuota);
    }

    /// <summary>
    /// The "previous values" half shared by save (lines 170-193) and delete (lines 244-266): finds the Code "1"
    /// quota covering the previous effective date and takes the previous forfeit off it, unfloored. Returns the
    /// row credited, or <c>null</c> when nothing was credited (not annual, or before the earliest evaluation
    /// date). Throws when an annual forfeit has no quota row to credit (lines 184-185, 257-258).
    /// </summary>
    private async Task<EmployeeAbsenceQuota> CreditPreviousQuotaAsync(PreviousValues previous, DateTime earliestEvaluationDate)
    {
        // Lines 170 / 244.
        if (previous.EffectiveDate < earliestEvaluationDate)
        {
            return null;
        }

        // Lines 173-175 / 247-248.
        var previousType = await _absenceTypeRepository.FirstOrDefaultAsync(previous.AbsenceTypeId);
        if (!IsAnnual(previousType))
        {
            return null;
        }

        // Lines 177 / 250: from here on "the previous type" IS the Code "1" type -- the message below names it.
        var annualType = await _absenceTypeRepository.FirstOrDefaultAsync(t => t.Code == AnnualLeaveAbsenceTypeCode);
        var quota = annualType == null ? null : FindQuota(previous.EmployeeId, annualType.Id, previous.EffectiveDate);
        if (quota == null)
        {
            var employee = await _employeeRepository.FirstOrDefaultAsync(previous.EmployeeId);
            throw new UserFriendlyException(L("NoLeaveQuotaFoundForEmployeeAndAbsenceType",
                CodeWithFullNameEn(employee), annualType?.NameEn));
        }

        // Lines 189 / 262: no floor.
        quota.QuotaForfeited = Math.Round(quota.QuotaForfeited - previous.QuotaForfeited, 6);
        return quota;
    }

    /// <summary>Legacy lines 179-182 / 207-210 / 252-255: the entitlement period covers the date; first match.</summary>
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

    private async Task<DateTime> GetEarliestEvaluationDateAsync()
    {
        var timeSettings = await _settings.GetTimeSettingsAsync();
        return timeSettings?.EarliestEvaluationDate ?? default;
    }

    private static bool IsAnnual(AbsenceType type)
    {
        return type != null && (type.Code == AnnualLeaveAbsenceTypeCode || type.QuotaType == QuotaType.AnnualLeave);
    }

    /// <summary>Legacy <c>EmployeeAbsenceQuota.QuotaBalance</c> (EmployeeAbsenceQuota.cs line 123), rounded to 2.</summary>
    private static double QuotaBalance(EmployeeAbsenceQuota q)
        => Math.Round(q.QuotaAccrued - (q.QuotaUsed + q.QuotaAdjusted + q.QuotaForfeited), 2);

    /// <summary>Legacy XPO computed <c>Employee.CodeWithFullNameEn</c> = <c>Code + ' ' + FullNameEn</c>.</summary>
    private static string CodeWithFullNameEn(Employee employee)
    {
        return string.IsNullOrWhiteSpace(employee?.FullNameEn)
            ? employee?.Code
            : employee.Code + " " + employee.FullNameEn;
    }

    private static EmployeeAbsQuotaForfeit MapToEntity(CreateOrEditEmployeeAbsQuotaForfeitDto input, EmployeeAbsQuotaForfeit entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.AbsenceTypeId = input.AbsenceTypeId;
        entity.EffectiveDate = input.EffectiveDate.Date;
        entity.QuotaForfeited = System.Math.Round(input.QuotaForfeited, 6);
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeAbsQuotaForfeitDto ToDto(EmployeeAbsQuotaForfeit entity)
    {
        return new EmployeeAbsQuotaForfeitDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            AbsenceTypeId = entity.AbsenceTypeId,
            AbsenceTypeName = entity.AbsenceType?.NameEn,
            EffectiveDate = entity.EffectiveDate,
            QuotaForfeited = entity.QuotaForfeited,
            Comments = entity.Comments
        };
    }

    /// <summary>Legacy's non-persistent <c>prevEmployee</c>/<c>prevAbsenceType</c>/<c>prevEffectiveDate</c>/
    /// <c>prevQuotaForfeited</c> (lines 65-86), set in <c>OnLoaded</c> (108-116).</summary>
    private sealed class PreviousValues
    {
        public int EmployeeId { get; private init; }
        public int AbsenceTypeId { get; private init; }
        public DateTime EffectiveDate { get; private init; }
        public double QuotaForfeited { get; private init; }

        public static PreviousValues Of(EmployeeAbsQuotaForfeit entity) => new()
        {
            EmployeeId = entity.EmployeeId,
            AbsenceTypeId = entity.AbsenceTypeId,
            EffectiveDate = entity.EffectiveDate,
            QuotaForfeited = entity.QuotaForfeited
        };
    }
}
