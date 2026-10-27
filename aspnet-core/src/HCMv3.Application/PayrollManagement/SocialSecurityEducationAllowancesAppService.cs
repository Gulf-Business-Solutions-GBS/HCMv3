using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.PayrollManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.PayrollManagement;

/// <summary>
/// Application service for <see cref="SocialSecurityEducationAllowance"/> (education-based allowance
/// steps of a <see cref="SocialSecurity"/> master). Ported from legacy XAF
/// SocialSecurityEducationAllowance business object.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances)]
public class SocialSecurityEducationAllowancesAppService : HCMv3AppServiceBase, ISocialSecurityEducationAllowancesAppService
{
    private readonly IRepository<SocialSecurityEducationAllowance> _repository;

    public SocialSecurityEducationAllowancesAppService(IRepository<SocialSecurityEducationAllowance> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SocialSecurityEducationAllowanceDto>> GetAll(GetAllSocialSecurityEducationAllowancesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.EducationBranch)
            .Include(a => a.Education)
            .WhereIf(input.SocialSecurityId.HasValue, a => a.SocialSecurityId == input.SocialSecurityId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SocialSecurityEducationAllowanceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSocialSecurityEducationAllowanceForEditOutput> GetSocialSecurityEducationAllowanceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSocialSecurityEducationAllowanceForEditOutput
        {
            SocialSecurityEducationAllowance = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSocialSecurityEducationAllowanceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSocialSecurityEducationAllowanceDto input)
    {
        var entity = MapToEntity(input, new SocialSecurityEducationAllowance());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityEducationAllowances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSocialSecurityEducationAllowanceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SocialSecurityEducationAllowance MapToEntity(CreateOrEditSocialSecurityEducationAllowanceDto input, SocialSecurityEducationAllowance entity)
    {
        entity.SocialSecurityId = input.SocialSecurityId;
        entity.EducationBranchId = input.EducationBranchId;
        entity.EducationId = input.EducationId;
        entity.Amount = input.Amount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EducationBranch/Education required, no duplicate
    /// (SocialSecurityId, EducationBranchId, EducationId) combination.
    /// </summary>
    private async Task ValidateAsync(SocialSecurityEducationAllowance entity)
    {
        if (entity.EducationBranchId == 0)
        {
            throw new UserFriendlyException(L("EducationBranchRequired"));
        }

        if (entity.EducationId == 0)
        {
            throw new UserFriendlyException(L("EducationRequired"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           a.SocialSecurityId == entity.SocialSecurityId &&
                           a.EducationBranchId == entity.EducationBranchId &&
                           a.EducationId == entity.EducationId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static SocialSecurityEducationAllowanceDto ToDto(SocialSecurityEducationAllowance entity)
    {
        return new SocialSecurityEducationAllowanceDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            EducationBranchId = entity.EducationBranchId,
            EducationBranchName = entity.EducationBranch?.NameEn,
            EducationId = entity.EducationId,
            EducationName = entity.Education?.NameEn,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSocialSecurityEducationAllowanceDto ToCreateOrEditDto(SocialSecurityEducationAllowance entity)
    {
        return new CreateOrEditSocialSecurityEducationAllowanceDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            EducationBranchId = entity.EducationBranchId,
            EducationId = entity.EducationId,
            Amount = entity.Amount
        };
    }
}
