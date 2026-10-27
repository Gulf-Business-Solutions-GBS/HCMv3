using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.TimeManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.TimeManagement;

/// <summary>
/// Application service for the Time Policy master (ported from legacy XAF TimePolicy business object).
/// Pure flat configuration with no FK relations, so every scalar field is mapped faithfully — no
/// business-logic simplification was needed for this entity.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_TimePolicies)]
public class TimePoliciesAppService : HCMv3AppServiceBase, ITimePoliciesAppService
{
    private readonly IRepository<TimePolicy> _timePolicyRepository;

    public TimePoliciesAppService(IRepository<TimePolicy> timePolicyRepository)
    {
        _timePolicyRepository = timePolicyRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<TimePolicyDto>> GetAll(GetAllTimePoliciesInput input)
    {
        input.Normalize();

        var query = _timePolicyRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(input.Filter) ||
                t.NameEn.Contains(input.Filter) ||
                t.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<TimePolicyDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetTimePolicyForEditOutput> GetTimePolicyForEdit(EntityDto input)
    {
        var entity = await _timePolicyRepository.GetAsync(input.Id);

        return new GetTimePolicyForEditOutput
        {
            TimePolicy = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditTimePolicyDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimePolicies_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _timePolicyRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimePolicies_Create)]
    protected virtual async Task CreateAsync(CreateOrEditTimePolicyDto input)
    {
        var entity = MapToEntity(input, new TimePolicy());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _timePolicyRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_TimePolicies_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditTimePolicyDto input)
    {
        var entity = await _timePolicyRepository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _timePolicyRepository.UpdateAsync(entity);
    }

    private static TimePolicy MapToEntity(CreateOrEditTimePolicyDto input, TimePolicy entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.ShouldSignIn = input.ShouldSignIn;
        entity.ShouldSignOut = input.ShouldSignOut;
        entity.ShouldBreakSignIn = input.ShouldBreakSignIn;
        entity.ShouldBreakSignOut = input.ShouldBreakSignOut;
        entity.CalcLateInAfterTolerance = input.CalcLateInAfterTolerance;
        entity.CalcEarlyOutAfterTolerance = input.CalcEarlyOutAfterTolerance;
        entity.LateInTolerance = input.LateInTolerance;
        entity.EarlyOutTolerance = input.EarlyOutTolerance;
        entity.CalcBreakLateInAfterTolerance = input.CalcBreakLateInAfterTolerance;
        entity.CalcBreakEarlyOutAfterTolerance = input.CalcBreakEarlyOutAfterTolerance;
        entity.BreakLateInTolerance = input.BreakLateInTolerance;
        entity.BreakEarlyOutTolerance = input.BreakEarlyOutTolerance;
        entity.ClockInLimit = input.ClockInLimit;
        entity.ClockOutLimit = input.ClockOutLimit;
        entity.BreakInLimit = input.BreakInLimit;
        entity.BreakOutLimit = input.BreakOutLimit;
        entity.MonthlyToleranceType = input.MonthlyToleranceType;
        entity.MonthlyAfterTolerance = input.MonthlyAfterTolerance;
        entity.TimeEventRule = input.TimeEventRule;
        entity.TimeMgmtStatus = input.TimeMgmtStatus;
        entity.OTMinLimit = input.OTMinLimit;
        entity.MarkWeekoffOnHoliday = input.MarkWeekoffOnHoliday;
        entity.AllowPunchInFromWorkLocationOnly = input.AllowPunchInFromWorkLocationOnly;
        entity.AllowPunchOutFromWorkLocationOnly = input.AllowPunchOutFromWorkLocationOnly;
        entity.IsFaceRequired = input.IsFaceRequired;
        entity.IsMissedPunchInNotiEnabled = input.IsMissedPunchInNotiEnabled;
        entity.MissedPunchInNotiTimeLimit = input.MissedPunchInNotiTimeLimit;
        entity.IsLatePunchInNotiEnabled = input.IsLatePunchInNotiEnabled;
        entity.LatePunchInNotiTimeLimit = input.LatePunchInNotiTimeLimit;
        entity.IsMissedPunchOutNotiEnabled = input.IsMissedPunchOutNotiEnabled;
        entity.MissedPunchOutNotiTimeLimit = input.MissedPunchOutNotiTimeLimit;
        entity.IsEarlyPunchOutNotiEnabled = input.IsEarlyPunchOutNotiEnabled;
        entity.EarlyPunchOutNotiTimeLimit = input.EarlyPunchOutNotiTimeLimit;
        entity.AllowPunchInFromExtWorklocation = input.AllowPunchInFromExtWorklocation;
        entity.AllowPunchOutFromExtWorklocation = input.AllowPunchOutFromExtWorklocation;
        entity.MonthlyLateInTolerance = input.MonthlyLateInTolerance;
        entity.MonthlyEarlyOutTolerance = input.MonthlyEarlyOutTolerance;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(TimePolicy entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _timePolicyRepository.GetAll()
            .Where(t => t.Code != null && t.Code != "")
            .Select(t => t.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        entity.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(TimePolicy entity)
    {
        var duplicateCode = await _timePolicyRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _timePolicyRepository.GetAll()
            .AnyAsync(t => t.Id != entity.Id &&
                           (t.NameEn == entity.NameEn || t.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static TimePolicyDto ToDto(TimePolicy entity)
    {
        return new TimePolicyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ShouldSignIn = entity.ShouldSignIn,
            ShouldSignOut = entity.ShouldSignOut,
            ShouldBreakSignIn = entity.ShouldBreakSignIn,
            ShouldBreakSignOut = entity.ShouldBreakSignOut,
            CalcLateInAfterTolerance = entity.CalcLateInAfterTolerance,
            CalcEarlyOutAfterTolerance = entity.CalcEarlyOutAfterTolerance,
            LateInTolerance = entity.LateInTolerance,
            EarlyOutTolerance = entity.EarlyOutTolerance,
            CalcBreakLateInAfterTolerance = entity.CalcBreakLateInAfterTolerance,
            CalcBreakEarlyOutAfterTolerance = entity.CalcBreakEarlyOutAfterTolerance,
            BreakLateInTolerance = entity.BreakLateInTolerance,
            BreakEarlyOutTolerance = entity.BreakEarlyOutTolerance,
            ClockInLimit = entity.ClockInLimit,
            ClockOutLimit = entity.ClockOutLimit,
            BreakInLimit = entity.BreakInLimit,
            BreakOutLimit = entity.BreakOutLimit,
            MonthlyToleranceType = entity.MonthlyToleranceType,
            MonthlyAfterTolerance = entity.MonthlyAfterTolerance,
            TimeEventRule = entity.TimeEventRule,
            TimeMgmtStatus = entity.TimeMgmtStatus,
            OTMinLimit = entity.OTMinLimit,
            MarkWeekoffOnHoliday = entity.MarkWeekoffOnHoliday,
            AllowPunchInFromWorkLocationOnly = entity.AllowPunchInFromWorkLocationOnly,
            AllowPunchOutFromWorkLocationOnly = entity.AllowPunchOutFromWorkLocationOnly,
            IsFaceRequired = entity.IsFaceRequired,
            IsMissedPunchInNotiEnabled = entity.IsMissedPunchInNotiEnabled,
            MissedPunchInNotiTimeLimit = entity.MissedPunchInNotiTimeLimit,
            IsLatePunchInNotiEnabled = entity.IsLatePunchInNotiEnabled,
            LatePunchInNotiTimeLimit = entity.LatePunchInNotiTimeLimit,
            IsMissedPunchOutNotiEnabled = entity.IsMissedPunchOutNotiEnabled,
            MissedPunchOutNotiTimeLimit = entity.MissedPunchOutNotiTimeLimit,
            IsEarlyPunchOutNotiEnabled = entity.IsEarlyPunchOutNotiEnabled,
            EarlyPunchOutNotiTimeLimit = entity.EarlyPunchOutNotiTimeLimit,
            AllowPunchInFromExtWorklocation = entity.AllowPunchInFromExtWorklocation,
            AllowPunchOutFromExtWorklocation = entity.AllowPunchOutFromExtWorklocation,
            MonthlyLateInTolerance = entity.MonthlyLateInTolerance,
            MonthlyEarlyOutTolerance = entity.MonthlyEarlyOutTolerance
        };
    }

    private static CreateOrEditTimePolicyDto ToCreateOrEditDto(TimePolicy entity)
    {
        return new CreateOrEditTimePolicyDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            ShouldSignIn = entity.ShouldSignIn,
            ShouldSignOut = entity.ShouldSignOut,
            ShouldBreakSignIn = entity.ShouldBreakSignIn,
            ShouldBreakSignOut = entity.ShouldBreakSignOut,
            CalcLateInAfterTolerance = entity.CalcLateInAfterTolerance,
            CalcEarlyOutAfterTolerance = entity.CalcEarlyOutAfterTolerance,
            LateInTolerance = entity.LateInTolerance,
            EarlyOutTolerance = entity.EarlyOutTolerance,
            CalcBreakLateInAfterTolerance = entity.CalcBreakLateInAfterTolerance,
            CalcBreakEarlyOutAfterTolerance = entity.CalcBreakEarlyOutAfterTolerance,
            BreakLateInTolerance = entity.BreakLateInTolerance,
            BreakEarlyOutTolerance = entity.BreakEarlyOutTolerance,
            ClockInLimit = entity.ClockInLimit,
            ClockOutLimit = entity.ClockOutLimit,
            BreakInLimit = entity.BreakInLimit,
            BreakOutLimit = entity.BreakOutLimit,
            MonthlyToleranceType = entity.MonthlyToleranceType,
            MonthlyAfterTolerance = entity.MonthlyAfterTolerance,
            TimeEventRule = entity.TimeEventRule,
            TimeMgmtStatus = entity.TimeMgmtStatus,
            OTMinLimit = entity.OTMinLimit,
            MarkWeekoffOnHoliday = entity.MarkWeekoffOnHoliday,
            AllowPunchInFromWorkLocationOnly = entity.AllowPunchInFromWorkLocationOnly,
            AllowPunchOutFromWorkLocationOnly = entity.AllowPunchOutFromWorkLocationOnly,
            IsFaceRequired = entity.IsFaceRequired,
            IsMissedPunchInNotiEnabled = entity.IsMissedPunchInNotiEnabled,
            MissedPunchInNotiTimeLimit = entity.MissedPunchInNotiTimeLimit,
            IsLatePunchInNotiEnabled = entity.IsLatePunchInNotiEnabled,
            LatePunchInNotiTimeLimit = entity.LatePunchInNotiTimeLimit,
            IsMissedPunchOutNotiEnabled = entity.IsMissedPunchOutNotiEnabled,
            MissedPunchOutNotiTimeLimit = entity.MissedPunchOutNotiTimeLimit,
            IsEarlyPunchOutNotiEnabled = entity.IsEarlyPunchOutNotiEnabled,
            EarlyPunchOutNotiTimeLimit = entity.EarlyPunchOutNotiTimeLimit,
            AllowPunchInFromExtWorklocation = entity.AllowPunchInFromExtWorklocation,
            AllowPunchOutFromExtWorklocation = entity.AllowPunchOutFromExtWorklocation,
            MonthlyLateInTolerance = entity.MonthlyLateInTolerance,
            MonthlyEarlyOutTolerance = entity.MonthlyEarlyOutTolerance
        };
    }
}
