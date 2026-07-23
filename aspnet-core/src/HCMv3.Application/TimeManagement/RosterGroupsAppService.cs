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
/// Application service for the Roster Group header (ported from legacy XAF RosterGroup business object).
/// Its child is <see cref="RosterGroupDetailsAppService"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroups)]
public class RosterGroupsAppService : HCMv3AppServiceBase, IRosterGroupsAppService
{
    private readonly IRepository<RosterGroup> _rosterGroupRepository;

    public RosterGroupsAppService(IRepository<RosterGroup> rosterGroupRepository)
    {
        _rosterGroupRepository = rosterGroupRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<RosterGroupDto>> GetAll(GetAllRosterGroupsInput input)
    {
        input.Normalize();

        var query = _rosterGroupRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), r =>
                r.Code.Contains(input.Filter) ||
                r.NameEn.Contains(input.Filter) ||
                r.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<RosterGroupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetRosterGroupForEditOutput> GetRosterGroupForEdit(EntityDto input)
    {
        var entity = await _rosterGroupRepository.GetAsync(input.Id);

        return new GetRosterGroupForEditOutput
        {
            RosterGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditRosterGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _rosterGroupRepository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditRosterGroupDto input)
    {
        var entity = new RosterGroup
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _rosterGroupRepository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_TimeManagement_RosterGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditRosterGroupDto input)
    {
        var entity = await _rosterGroupRepository.GetAsync(input.Id.Value);

        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateUniquenessAsync(entity);

        await _rosterGroupRepository.UpdateAsync(entity);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(RosterGroup entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _rosterGroupRepository.GetAll()
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
    private async Task ValidateUniquenessAsync(RosterGroup entity)
    {
        var duplicateCode = await _rosterGroupRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id && r.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _rosterGroupRepository.GetAll()
            .AnyAsync(r => r.Id != entity.Id &&
                           (r.NameEn == entity.NameEn || r.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static RosterGroupDto ToDto(RosterGroup entity)
    {
        return new RosterGroupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }

    private static CreateOrEditRosterGroupDto ToCreateOrEditDto(RosterGroup entity)
    {
        return new CreateOrEditRosterGroupDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr
        };
    }
}
