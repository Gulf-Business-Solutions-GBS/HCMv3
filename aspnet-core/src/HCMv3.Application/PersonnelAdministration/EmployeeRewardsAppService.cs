using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.PersonnelAdministration.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PersonnelAdministration;

/// <summary>
/// Application service for <see cref="EmployeeReward"/> (ported from legacy XAF EmployeeReward business
/// object, Wave 4 deferred bucket). Ports the essential required-field validation only (enforced via
/// DTO Data Annotations).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards)]
public class EmployeeRewardsAppService : HCMv3AppServiceBase, IEmployeeRewardsAppService
{
    private readonly IRepository<EmployeeReward> _repository;

    public EmployeeRewardsAppService(IRepository<EmployeeReward> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<EmployeeRewardDto>> GetAll(GetAllEmployeeRewardsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(e => e.Employee)
            .WhereIf(input.EmployeeId.HasValue, e => e.EmployeeId == input.EmployeeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<EmployeeRewardDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetEmployeeRewardForEditOutput> GetEmployeeRewardForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetEmployeeRewardForEditOutput
        {
            EmployeeReward = new CreateOrEditEmployeeRewardDto
            {
                Id = entity.Id,
                EmployeeId = entity.EmployeeId,
                RewardType = entity.RewardType,
                RewardDate = entity.RewardDate,
                Certificate = entity.Certificate,
                Comments = entity.Comments
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditEmployeeRewardDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards_Create)]
    protected virtual async Task CreateAsync(CreateOrEditEmployeeRewardDto input)
    {
        var entity = MapToEntity(input, new EmployeeReward());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditEmployeeRewardDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PersonnelAdministration_EmployeeRewards_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static EmployeeReward MapToEntity(CreateOrEditEmployeeRewardDto input, EmployeeReward entity)
    {
        entity.EmployeeId = input.EmployeeId;
        entity.RewardType = input.RewardType;
        entity.RewardDate = input.RewardDate.Date;
        entity.Certificate = input.Certificate;
        entity.Comments = input.Comments;
        return entity;
    }

    private static EmployeeRewardDto ToDto(EmployeeReward entity)
    {
        return new EmployeeRewardDto
        {
            Id = entity.Id,
            EmployeeId = entity.EmployeeId,
            EmployeeCode = entity.Employee?.Code,
            EmployeeFullNameEn = entity.Employee?.FullNameEn,
            RewardType = entity.RewardType,
            RewardDate = entity.RewardDate,
            Certificate = entity.Certificate,
            Comments = entity.Comments
        };
    }
}
