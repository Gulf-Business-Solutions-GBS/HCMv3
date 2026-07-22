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
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the Nationality master (ported from legacy XAF Nationality business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly) since
/// the list DTO also needs the FK's display name (NationalityGroupName) resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Nationalities)]
public class NationalitiesAppService : HCMv3AppServiceBase, INationalitiesAppService
{
    private readonly IRepository<Nationality> _nationalityRepository;

    public NationalitiesAppService(IRepository<Nationality> nationalityRepository)
    {
        _nationalityRepository = nationalityRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NationalityDto>> GetAll(GetAllNationalitiesInput input)
    {
        input.Normalize();

        var query = _nationalityRepository.GetAll()
            .Include(n => n.NationalityGroup)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), n =>
                n.Code.Contains(input.Filter) ||
                n.NameEn.Contains(input.Filter) ||
                n.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NationalityDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetNationalityForEditOutput> GetNationalityForEdit(EntityDto input)
    {
        var nationality = await _nationalityRepository.GetAsync(input.Id);

        return new GetNationalityForEditOutput
        {
            Nationality = ToCreateOrEditDto(nationality)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNationalityDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Nationalities_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNationalityDto input)
    {
        var nationality = new Nationality
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            NationalityGroupId = input.NationalityGroupId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(nationality);
        await ValidateUniquenessAsync(nationality);

        await _nationalityRepository.InsertAsync(nationality);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Nationalities_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNationalityDto input)
    {
        var nationality = await _nationalityRepository.GetAsync(input.Id.Value);

        nationality.Code = input.Code;
        nationality.NameEn = input.NameEn;
        nationality.NameAr = input.NameAr;
        nationality.NationalityGroupId = input.NationalityGroupId;

        await AssignCodeIfEmptyAsync(nationality);
        await ValidateUniquenessAsync(nationality);

        await _nationalityRepository.UpdateAsync(nationality);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Nationalities_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _nationalityRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Nationality nationality)
    {
        if (!nationality.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _nationalityRepository.GetAll()
            .Where(n => n.Code != null && n.Code != "")
            .Select(n => n.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        nationality.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(Nationality nationality)
    {
        var duplicateCode = await _nationalityRepository.GetAll()
            .AnyAsync(n => n.Id != nationality.Id && n.Code == nationality.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _nationalityRepository.GetAll()
            .AnyAsync(n => n.Id != nationality.Id &&
                           (n.NameEn == nationality.NameEn || n.NameAr == nationality.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static NationalityDto ToDto(Nationality nationality)
    {
        return new NationalityDto
        {
            Id = nationality.Id,
            Code = nationality.Code,
            NameEn = nationality.NameEn,
            NameAr = nationality.NameAr,
            NationalityGroupId = nationality.NationalityGroupId,
            NationalityGroupName = nationality.NationalityGroup?.NameEn
        };
    }

    private static CreateOrEditNationalityDto ToCreateOrEditDto(Nationality nationality)
    {
        return new CreateOrEditNationalityDto
        {
            Id = nationality.Id,
            Code = nationality.Code,
            NameEn = nationality.NameEn,
            NameAr = nationality.NameAr,
            NationalityGroupId = nationality.NationalityGroupId
        };
    }
}
