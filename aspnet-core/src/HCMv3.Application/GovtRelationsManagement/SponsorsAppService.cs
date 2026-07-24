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
using HCMv3.GovtRelationsManagement.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GovtRelationsManagement;

/// <summary>
/// Application service for the <see cref="Sponsor"/> aggregate root (ported from legacy XAF Sponsor
/// business object). See <see cref="Sponsor"/> remarks for the flat parent+child modeling of its 4
/// aggregated child collections.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Sponsors)]
public class SponsorsAppService : HCMv3AppServiceBase, ISponsorsAppService
{
    private readonly IRepository<Sponsor> _repository;

    public SponsorsAppService(IRepository<Sponsor> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SponsorDto>> GetAll(GetAllSponsorsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(s => s.Nationality)
            .Include(s => s.BusinessType)
            .Include(s => s.OrganizationUnit)
            .Include(s => s.Governorate)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), s =>
                s.Code.Contains(input.Filter) ||
                s.NameEn.Contains(input.Filter) ||
                s.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SponsorDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetSponsorForEditOutput> GetSponsorForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetSponsorForEditOutput
        {
            Sponsor = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSponsorDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Sponsors_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Sponsors_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSponsorDto input)
    {
        var entity = MapToEntity(input, new Sponsor());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GovtRelationsManagement_Sponsors_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSponsorDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await AssignCodeIfEmptyAsync(entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private static Sponsor MapToEntity(CreateOrEditSponsorDto input, Sponsor entity)
    {
        entity.Code = input.Code;
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.Email = input.Email;
        entity.Gender = input.Gender;
        entity.Profession = input.Profession;
        entity.Building = input.Building;
        entity.Street = input.Street;
        entity.Area = input.Area;
        entity.Block = input.Block;
        entity.Telephone = input.Telephone;
        entity.NationalityId = input.NationalityId;
        entity.BusinessTypeId = input.BusinessTypeId;
        entity.OrganizationUnitId = input.OrganizationUnitId;
        entity.UnifiedNum = input.UnifiedNum;
        entity.SocialSecurityNum = input.SocialSecurityNum;
        entity.MOSALID = input.MOSALID;
        entity.GovernorateId = input.GovernorateId;
        entity.BusinessOwnerNameEn = input.BusinessOwnerNameEn;
        entity.BusinessOwnerNameAr = input.BusinessOwnerNameAr;
        entity.Fax = input.Fax;
        entity.POBox = input.POBox;

        return entity;
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Sponsor entity)
    {
        if (!entity.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _repository.GetAll()
            .Where(s => s.Code != null && s.Code != "")
            .Select(s => s.Code)
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
    private async Task ValidateAsync(Sponsor entity)
    {
        var duplicateCode = await _repository.GetAll()
            .AnyAsync(s => s.Id != entity.Id && s.Code == entity.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _repository.GetAll()
            .AnyAsync(s => s.Id != entity.Id &&
                           (s.NameEn == entity.NameEn || s.NameAr == entity.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static SponsorDto ToDto(Sponsor entity)
    {
        return new SponsorDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Email = entity.Email,
            Gender = entity.Gender,
            Profession = entity.Profession,
            Building = entity.Building,
            Street = entity.Street,
            Area = entity.Area,
            Block = entity.Block,
            Telephone = entity.Telephone,
            NationalityId = entity.NationalityId,
            NationalityName = entity.Nationality?.NameEn,
            BusinessTypeId = entity.BusinessTypeId,
            BusinessTypeName = entity.BusinessType?.NameEn,
            OrganizationUnitId = entity.OrganizationUnitId,
            OrganizationUnitName = entity.OrganizationUnit?.DisplayName,
            UnifiedNum = entity.UnifiedNum,
            SocialSecurityNum = entity.SocialSecurityNum,
            MOSALID = entity.MOSALID,
            GovernorateId = entity.GovernorateId,
            GovernorateName = entity.Governorate?.NameEn,
            BusinessOwnerNameEn = entity.BusinessOwnerNameEn,
            BusinessOwnerNameAr = entity.BusinessOwnerNameAr,
            Fax = entity.Fax,
            POBox = entity.POBox
        };
    }

    private static CreateOrEditSponsorDto ToCreateOrEditDto(Sponsor entity)
    {
        return new CreateOrEditSponsorDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            Email = entity.Email,
            Gender = entity.Gender,
            Profession = entity.Profession,
            Building = entity.Building,
            Street = entity.Street,
            Area = entity.Area,
            Block = entity.Block,
            Telephone = entity.Telephone,
            NationalityId = entity.NationalityId,
            BusinessTypeId = entity.BusinessTypeId,
            OrganizationUnitId = entity.OrganizationUnitId,
            UnifiedNum = entity.UnifiedNum,
            SocialSecurityNum = entity.SocialSecurityNum,
            MOSALID = entity.MOSALID,
            GovernorateId = entity.GovernorateId,
            BusinessOwnerNameEn = entity.BusinessOwnerNameEn,
            BusinessOwnerNameAr = entity.BusinessOwnerNameAr,
            Fax = entity.Fax,
            POBox = entity.POBox
        };
    }
}
