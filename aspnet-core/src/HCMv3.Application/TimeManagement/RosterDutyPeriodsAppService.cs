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
/// Application service for the Roster subsystem's header, parallel to <see cref="DutyPeriodsAppService"/>
/// (ported from legacy XAF RosterDutyPeriod business object). Its child is
/// <see cref="RosterDutyPeriodDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriods)]
public class RosterDutyPeriodsAppService : HCMv3AppServiceBase, IRosterDutyPeriodsAppService
{
    private readonly IRepository<RosterDutyPeriod> _rosterDutyPeriodRepository;

    public RosterDutyPeriodsAppService(IRepository<RosterDutyPeriod> rosterDutyPeriodRepository)
    {
        _rosterDutyPeriodRepository = rosterDutyPeriodRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterDutyPeriodDto>> GetAll(GetAllRosterDutyPeriodsInput input)
    {
        input.Normalize();

        var query = _rosterDutyPeriodRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterDutyPeriodDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterDutyPeriodForEditOutput> GetRosterDutyPeriodForEdit(EntityDto input)
    {
        var entity = await _rosterDutyPeriodRepository.GetAsync(input.Id);

        return new GetRosterDutyPeriodForEditOutput
        {
            RosterDutyPeriod = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterDutyPeriodDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriods_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterDutyPeriodRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriods_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterDutyPeriodDto input)
    {
        var entity = new RosterDutyPeriod
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _rosterDutyPeriodRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterDutyPeriods_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterDutyPeriodDto input)
    {
        var entity = await _rosterDutyPeriodRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _rosterDutyPeriodRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(RosterDutyPeriod entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _rosterDutyPeriodRepository.GetAll()
            .Where(r => r.Code != null && r.Code != "")
            .Select(r => r.Code)
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
    private async Task ValidateUniquenessAsync(RosterDutyPeriod entity)
    {
        var duplicateCode = await _rosterDutyPeriodRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _rosterDutyPeriodRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static RosterDutyPeriodDto ToDto(RosterDutyPeriod entity)
    {
        return new RosterDutyPeriodDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditRosterDutyPeriodDto ToCreateOrEditDto(RosterDutyPeriod entity)
    {
        return new CreateOrEditRosterDutyPeriodDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
