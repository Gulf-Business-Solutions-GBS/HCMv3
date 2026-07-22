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
/// Application service for the Institution master (ported from legacy XAF Institution business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly) since
/// the list DTO also needs the FK's display name (UniversityName) resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Institutions)]
public class InstitutionsAppService : HCMv3AppServiceBase, IInstitutionsAppService
{
    private readonly IRepository<Institution> _institutionRepository;

    public InstitutionsAppService(IRepository<Institution> institutionRepository)
    {
        _institutionRepository = institutionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<InstitutionDto>> GetAll(GetAllInstitutionsInput input)
    {
        input.Normalize();

        var query = _institutionRepository.GetAll()
            .Include(i => i.University)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), i =>
                i.Code.Contains(input.Filter) ||
                i.NameEn.Contains(input.Filter) ||
                i.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<InstitutionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetInstitutionForEditOutput> GetInstitutionForEdit(EntityDto input)
    {
        var institution = await _institutionRepository.GetAsync(input.Id);

        return new GetInstitutionForEditOutput
        {
            Institution = ToCreateOrEditDto(institution)
        };
    }

    public async Task CreateOrEdit(CreateOrEditInstitutionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Institutions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditInstitutionDto input)
    {
        var institution = new Institution
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            UniversityId = input.UniversityId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(institution);
        await ValidateUniquenessAsync(institution);

        await _institutionRepository.InsertAsync(institution);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Institutions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditInstitutionDto input)
    {
        var institution = await _institutionRepository.GetAsync(input.Id.Value);

        institution.Code = input.Code;
        institution.NameEn = input.NameEn;
        institution.NameAr = input.NameAr;
        institution.UniversityId = input.UniversityId;

        await AssignCodeIfEmptyAsync(institution);
        await ValidateUniquenessAsync(institution);

        await _institutionRepository.UpdateAsync(institution);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_Institutions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _institutionRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(Institution institution)
    {
        if (!institution.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _institutionRepository.GetAll()
            .Where(i => i.Code != null && i.Code != "")
            .Select(i => i.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        institution.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(Institution institution)
    {
        var duplicateCode = await _institutionRepository.GetAll()
            .AnyAsync(i => i.Id != institution.Id && i.Code == institution.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _institutionRepository.GetAll()
            .AnyAsync(i => i.Id != institution.Id &&
                           (i.NameEn == institution.NameEn || i.NameAr == institution.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static InstitutionDto ToDto(Institution institution)
    {
        return new InstitutionDto
        {
            Id = institution.Id,
            Code = institution.Code,
            NameEn = institution.NameEn,
            NameAr = institution.NameAr,
            UniversityId = institution.UniversityId,
            UniversityName = institution.University?.NameEn
        };
    }

    private static CreateOrEditInstitutionDto ToCreateOrEditDto(Institution institution)
    {
        return new CreateOrEditInstitutionDto
        {
            Id = institution.Id,
            Code = institution.Code,
            NameEn = institution.NameEn,
            NameAr = institution.NameAr,
            UniversityId = institution.UniversityId
        };
    }
}
