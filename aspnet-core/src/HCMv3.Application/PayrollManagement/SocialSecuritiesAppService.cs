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
/// Application service for the Social Security master/header (ported from legacy XAF SocialSecurity
/// business object). See <see cref="SocialSecurity"/> class remarks for why the legacy's four aggregated
/// child collections are ported as separate top-level entities/app services instead.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurities)]
public class SocialSecuritiesAppService : HCMv3AppServiceBase, ISocialSecuritiesAppService
{
    private readonly IRepository<SocialSecurity> _repository;

    public SocialSecuritiesAppService(IRepository<SocialSecurity> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SocialSecurityDto>> GetAll(GetAllSocialSecuritiesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Nationality)
            .WhereIf(input.NationalityId.HasValue, s => s.NationalityId == input.NationalityId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SocialSecurityDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSocialSecurityForEditOutput> GetSocialSecurityForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSocialSecurityForEditOutput
        {
            SocialSecurity = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSocialSecurityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurities_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSocialSecurityDto input)
    {
        var entity = MapToEntity(input, new SocialSecurity());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSocialSecurityDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SocialSecurity MapToEntity(CreateOrEditSocialSecurityDto input, SocialSecurity entity)
    {
        entity.NationalityId = input.NationalityId;
        entity.EarningsCap = input.EarningsCap;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.ChildAllowance = input.ChildAllowance;
        entity.SpecialNeedChildAllowance = input.SpecialNeedChildAllowance;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Nationality required, StartDate/EndDate required and
    /// sane (EndDate &gt;= StartDate).
    /// </summary>
    private void ValidateAsync(SocialSecurity entity)
    {
        if (entity.NationalityId == 0)
        {
            throw new UserFriendlyException(L("NationalityRequired"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static SocialSecurityDto ToDto(SocialSecurity entity)
    {
        return new SocialSecurityDto
        {
            Id = entity.Id,
            NationalityId = entity.NationalityId,
            NationalityName = entity.Nationality?.NameEn,
            EarningsCap = entity.EarningsCap,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChildAllowance = entity.ChildAllowance,
            SpecialNeedChildAllowance = entity.SpecialNeedChildAllowance
        };
    }

    private static CreateOrEditSocialSecurityDto ToCreateOrEditDto(SocialSecurity entity)
    {
        return new CreateOrEditSocialSecurityDto
        {
            Id = entity.Id,
            NationalityId = entity.NationalityId,
            EarningsCap = entity.EarningsCap,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            ChildAllowance = entity.ChildAllowance,
            SpecialNeedChildAllowance = entity.SpecialNeedChildAllowance
        };
    }
}
