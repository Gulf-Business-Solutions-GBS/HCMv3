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
/// Application service for <see cref="SocialSecurityRule"/> (qualification/gender/marital/dependant-based
/// allowance rules of a <see cref="SocialSecurity"/> master). Ported from legacy XAF SocialSecurityRule
/// business object. Unlike its <see cref="SocialSecurityDetail"/> sibling, the legacy source has NO
/// date-range overlap check for this entity -- intentionally not added here either.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityRules)]
public class SocialSecurityRulesAppService : HCMv3AppServiceBase, ISocialSecurityRulesAppService
{
    private readonly IRepository<SocialSecurityRule> _repository;

    public SocialSecurityRulesAppService(IRepository<SocialSecurityRule> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SocialSecurityRuleDto>> GetAll(GetAllSocialSecurityRulesInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(r => r.Qualification)
            .Include(r => r.MaritalStatus)
            .WhereIf(input.SocialSecurityId.HasValue, r => r.SocialSecurityId == input.SocialSecurityId);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SocialSecurityRuleDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSocialSecurityRuleForEditOutput> GetSocialSecurityRuleForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSocialSecurityRuleForEditOutput
        {
            SocialSecurityRule = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSocialSecurityRuleDto input)
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

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityRules_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityRules_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSocialSecurityRuleDto input)
    {
        var entity = MapToEntity(input, new SocialSecurityRule());
        entity.TenantId = AbpSession.TenantId ?? 0;

        ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_PayrollManagement_SocialSecurityRules_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSocialSecurityRuleDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static SocialSecurityRule MapToEntity(CreateOrEditSocialSecurityRuleDto input, SocialSecurityRule entity)
    {
        entity.SocialSecurityId = input.SocialSecurityId;
        entity.StartDate = input.StartDate.Date;
        entity.EndDate = input.EndDate.Date;
        entity.QualificationId = input.QualificationId;
        entity.Gender = input.Gender;
        entity.MaritalStatusId = input.MaritalStatusId;
        entity.Dependant = input.Dependant;
        entity.Disability = input.Disability;
        entity.Amount = input.Amount;

        return entity;
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: Qualification required, StartDate/EndDate required and
    /// sane (EndDate &gt;= StartDate). NO date-range overlap check here (legacy has none for this
    /// entity, unlike <see cref="SocialSecurityDetail"/>).
    /// </summary>
    private void ValidateAsync(SocialSecurityRule entity)
    {
        if (entity.QualificationId == 0)
        {
            throw new UserFriendlyException(L("QualificationRequired"));
        }

        if (entity.EndDate < entity.StartDate)
        {
            throw new UserFriendlyException(L("EndDateCannotBeEarlierThanStartDate"));
        }
    }

    private static SocialSecurityRuleDto ToDto(SocialSecurityRule entity)
    {
        return new SocialSecurityRuleDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            QualificationId = entity.QualificationId,
            QualificationName = entity.Qualification?.NameEn,
            Gender = entity.Gender,
            MaritalStatusId = entity.MaritalStatusId,
            MaritalStatusName = entity.MaritalStatus?.NameEn,
            Dependant = entity.Dependant,
            Disability = entity.Disability,
            Amount = entity.Amount
        };
    }

    private static CreateOrEditSocialSecurityRuleDto ToCreateOrEditDto(SocialSecurityRule entity)
    {
        return new CreateOrEditSocialSecurityRuleDto
        {
            Id = entity.Id,
            SocialSecurityId = entity.SocialSecurityId,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            QualificationId = entity.QualificationId,
            Gender = entity.Gender,
            MaritalStatusId = entity.MaritalStatusId,
            Dependant = entity.Dependant,
            Disability = entity.Disability,
            Amount = entity.Amount
        };
    }
}
