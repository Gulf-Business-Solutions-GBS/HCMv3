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
/// Application service for <see cref="SocialSecurityDetail"/> (earnings-tier contribution steps of a
/// <see cref="SocialSecurity"/> master). Ported from legacy XAF SocialSecurityDetail business object.
/// The legacy's redundant duplicate overlap check (two near-identical LINQ blocks that check the exact
/// same condition) is ported once, not twice.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails)]
public class SocialSecurityDetailsAppService : HCMv3AppServiceBase, ISocialSecurityDetailsAppService
{
    private readonly IRepository<SocialSecurityDetail> _repository;

    public SocialSecurityDetailsAppService(IRepository<SocialSecurityDetail> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SocialSecurityDetailDto>> GetAll(GetAllSocialSecurityDetailsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .WhereIf(input.SocialSecurityId.HasValue, d => d.SocialSecurityId == input.SocialSecurityId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SocialSecurityDetailDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSocialSecurityDetailForEditOutput> GetSocialSecurityDetailForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSocialSecurityDetailForEditOutput
        {
            SocialSecurityDetail = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSocialSecurityDetailDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSocialSecurityDetailDto input)
    {
        var entity = MapToEntity(input, new SocialSecurityDetail());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityDetails_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSocialSecurityDetailDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SocialSecurityDetail MapToEntity(CreateOrEditSocialSecurityDetailDto input, SocialSecurityDetail entity)
    {
        entity.SocialSecurityId = input.SocialSecurityId;
        entity.MinEarnings = input.MinEarnings;
        entity.MaxEarnings = input.MaxEarnings;
        entity.EmployeeContribution = input.EmployeeContribution;
        entity.EmployerContribution = input.EmployerContribution;
        entity.MinEarningsEmployeeContribution = input.MinEarningsEmployeeContribution;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: MaxEarnings &gt;= MinEarnings, and no earnings-tier
    /// overlap against sibling details of the same parent <see cref="SocialSecurity"/> (ported once,
    /// not the legacy's redundant duplicate check).
    /// </summary>
    private async Task ValidateAsync(SocialSecurityDetail entity)
    {
        if (entity.MaxEarnings < entity.MinEarnings)
        {
            throw new UserFriendlyException(L("MaxEarningsCannotBeLessThanMinEarnings"));
        }

        var overlaps = await _repository.GetAll()
            .AnyAsync(d => d.Id != entity.Id &&
                           d.SocialSecurityId == entity.SocialSecurityId &&
                           !(d.MinEarnings > entity.MaxEarnings || d.MaxEarnings < entity.MinEarnings));
        if (overlaps)
        {
            throw new UserFriendlyException(L("DuplicateRecordFound"));
        }
    }

    private static SocialSecurityDetailDto ToDto(SocialSecurityDetail entity)
    {
        return new SocialSecurityDetailDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            MinEarnings = entity.MinEarnings,
            MaxEarnings = entity.MaxEarnings,
            EmployeeContribution = entity.EmployeeContribution,
            EmployerContribution = entity.EmployerContribution,
            MinEarningsEmployeeContribution = entity.MinEarningsEmployeeContribution
        };
    }

    private static CreateOrEditSocialSecurityDetailDto ToCreateOrEditDto(SocialSecurityDetail entity)
    {
        return new CreateOrEditSocialSecurityDetailDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            MinEarnings = entity.MinEarnings,
            MaxEarnings = entity.MaxEarnings,
            EmployeeContribution = entity.EmployeeContribution,
            EmployerContribution = entity.EmployerContribution,
            MinEarningsEmployeeContribution = entity.MinEarningsEmployeeContribution
        };
    }
}
