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
/// Application service for the PersonalActionReason master (ported from legacy XAF PersonalActionReason
/// business object). The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks)
/// lives here, out of the persistence lifecycle. Entity/DTO mapping is done manually (not via Mapperly)
/// since the list DTO also needs the FK's display name (PersonalActionName) resolved.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons)]
public class PersonalActionReasonsAppService : HCMv3AppServiceBase, IPersonalActionReasonsAppService
{
    private readonly IRepository<PersonalActionReason> _personalActionReasonRepository;

    public PersonalActionReasonsAppService(IRepository<PersonalActionReason> personalActionReasonRepository)
    {
        _personalActionReasonRepository = personalActionReasonRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PersonalActionReasonDto>> GetAll(GetAllPersonalActionReasonsInput input)
    {
        input.Normalize();

        var query = _personalActionReasonRepository.GetAll()
            .Include(p => p.PersonalAction)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p =>
                p.Code.Contains(input.Filter) ||
                p.NameEn.Contains(input.Filter) ||
                p.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PersonalActionReasonDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPersonalActionReasonForEditOutput> GetPersonalActionReasonForEdit(EntityDto input)
    {
        var personalActionReason = await _personalActionReasonRepository.GetAsync(input.Id);

        return new GetPersonalActionReasonForEditOutput
        {
            PersonalActionReason = ToCreateOrEditDto(personalActionReason)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPersonalActionReasonDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPersonalActionReasonDto input)
    {
        var personalActionReason = new PersonalActionReason
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            PersonalActionId = input.PersonalActionId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(personalActionReason);
        await ValidateUniquenessAsync(personalActionReason);

        await _personalActionReasonRepository.InsertAsync(personalActionReason);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPersonalActionReasonDto input)
    {
        var personalActionReason = await _personalActionReasonRepository.GetAsync(input.Id.Value);

        personalActionReason.Code = input.Code;
        personalActionReason.NameEn = input.NameEn;
        personalActionReason.NameAr = input.NameAr;
        personalActionReason.PersonalActionId = input.PersonalActionId;

        await AssignCodeIfEmptyAsync(personalActionReason);
        await ValidateUniquenessAsync(personalActionReason);

        await _personalActionReasonRepository.UpdateAsync(personalActionReason);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActionReasons_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _personalActionReasonRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(PersonalActionReason personalActionReason)
    {
        if (!personalActionReason.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _personalActionReasonRepository.GetAll()
            .Where(p => p.Code != null && p.Code != "")
            .Select(p => p.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        personalActionReason.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(PersonalActionReason personalActionReason)
    {
        var duplicateCode = await _personalActionReasonRepository.GetAll()
            .AnyAsync(p => p.Id != personalActionReason.Id && p.Code == personalActionReason.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _personalActionReasonRepository.GetAll()
            .AnyAsync(p => p.Id != personalActionReason.Id &&
                           (p.NameEn == personalActionReason.NameEn || p.NameAr == personalActionReason.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static PersonalActionReasonDto ToDto(PersonalActionReason personalActionReason)
    {
        return new PersonalActionReasonDto
        {
            Id = personalActionReason.Id,
            Code = personalActionReason.Code,
            NameEn = personalActionReason.NameEn,
            NameAr = personalActionReason.NameAr,
            PersonalActionId = personalActionReason.PersonalActionId,
            PersonalActionName = personalActionReason.PersonalAction?.NameEn
        };
    }

    private static CreateOrEditPersonalActionReasonDto ToCreateOrEditDto(PersonalActionReason personalActionReason)
    {
        return new CreateOrEditPersonalActionReasonDto
        {
            Id = personalActionReason.Id,
            Code = personalActionReason.Code,
            NameEn = personalActionReason.NameEn,
            NameAr = personalActionReason.NameAr,
            PersonalActionId = personalActionReason.PersonalActionId
        };
    }
}
