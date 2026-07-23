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
/// Application service for <see cref="SocialSecurityMaritalAllowance"/> (marital-status-based allowance
/// steps of a <see cref="SocialSecurity"/> master). Ported from legacy XAF
/// SocialSecurityMaritalAllowance business object.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances)]
public class SocialSecurityMaritalAllowancesAppService : HCMv3AppServiceBase, ISocialSecurityMaritalAllowancesAppService
{
    private readonly IRepository<SocialSecurityMaritalAllowance> _repository;

    public SocialSecurityMaritalAllowancesAppService(IRepository<SocialSecurityMaritalAllowance> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SocialSecurityMaritalAllowanceDto>> GetAll(GetAllSocialSecurityMaritalAllowancesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(a => a.EducationBranch)
            .Include(a => a.MaritalStatus)
            .WhereIf(input.SocialSecurityId.HasValue, a => a.SocialSecurityId == input.SocialSecurityId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SocialSecurityMaritalAllowanceDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSocialSecurityMaritalAllowanceForEditOutput> GetSocialSecurityMaritalAllowanceForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSocialSecurityMaritalAllowanceForEditOutput
        {
            SocialSecurityMaritalAllowance = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSocialSecurityMaritalAllowanceDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSocialSecurityMaritalAllowanceDto input)
    {
        var entity = MapToEntity(input, new SocialSecurityMaritalAllowance());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityMaritalAllowances_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSocialSecurityMaritalAllowanceDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SocialSecurityMaritalAllowance MapToEntity(CreateOrEditSocialSecurityMaritalAllowanceDto input, SocialSecurityMaritalAllowance entity)
    {
        entity.SocialSecurityId = input.SocialSecurityId;
        entity.EducationBranchId = input.EducationBranchId;
        entity.MaritalStatusId = input.MaritalStatusId;
        entity.Amount = input.Amount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: EducationBranch/MaritalStatus required, no duplicate
    /// (SocialSecurityId, EducationBranchId, MaritalStatusId) combination.
    /// </summary>
    private async Task ValidateAsync(SocialSecurityMaritalAllowance entity)
    {
        if (entity.EducationBranchId == 0)
        {
            throw new UserFriendlyException(L("EducationBranchRequired"));
        }

        if (entity.MaritalStatusId == 0)
        {
            throw new UserFriendlyException(L("MaritalStatusRequired"));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(a => a.Id != entity.Id &&
                           a.SocialSecurityId == entity.SocialSecurityId &&
                           a.EducationBranchId == entity.EducationBranchId &&
                           a.MaritalStatusId == entity.MaritalStatusId);
        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static SocialSecurityMaritalAllowanceDto ToDto(SocialSecurityMaritalAllowance entity)
    {
        return new SocialSecurityMaritalAllowanceDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            EducationBranchId = entity.EducationBranchId,
            EducationBranchName = entity.EducationBranch?.NameEn,
            MaritalStatusId = entity.MaritalStatusId,
            MaritalStatusName = entity.MaritalStatus?.NameEn,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSocialSecurityMaritalAllowanceDto ToCreateOrEditDto(SocialSecurityMaritalAllowance entity)
    {
        return new CreateOrEditSocialSecurityMaritalAllowanceDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            EducationBranchId = entity.EducationBranchId,
            MaritalStatusId = entity.MaritalStatusId,
            Amount = entity.Amount
        };
    }
}
