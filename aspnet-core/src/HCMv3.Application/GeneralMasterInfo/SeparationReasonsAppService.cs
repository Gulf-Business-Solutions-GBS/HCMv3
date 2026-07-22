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
/// Application service for the SeparationReason master (ported from legacy XAF SeparationReason business object).
/// The legacy <c>PerformValidation</c>/<c>OnDeleting</c> logic (auto Code, duplicate Code/Name checks,
/// system-record edit/delete protection) lives here, out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons)]
public class SeparationReasonsAppService : HCMv3AppServiceBase, ISeparationReasonsAppService
{
    private readonly IRepository<SeparationReason> _separationReasonRepository;

    public SeparationReasonsAppService(IRepository<SeparationReason> separationReasonRepository)
    {
        _separationReasonRepository = separationReasonRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<SeparationReasonDto>> GetAll(GetAllSeparationReasonsInput input)
    {
        input.Normalize();

        var query = _separationReasonRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), s =>
                s.Code.Contains(input.Filter) ||
                s.NameEn.Contains(input.Filter) ||
                s.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<SeparationReasonDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetSeparationReasonForEditOutput> GetSeparationReasonForEdit(EntityDto input)
    {
        var separationReason = await _separationReasonRepository.GetAsync(input.Id);

        return new GetSeparationReasonForEditOutput
        {
            SeparationReason = ToCreateOrEditDto(separationReason)
        };
    }

    public async Task CreateOrEdit(CreateOrEditSeparationReasonDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons_Create)]
    protected virtual async Task CreateAsync(CreateOrEditSeparationReasonDto input)
    {
        var separationReason = new SeparationReason
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            IsSystemRecord = input.IsSystemRecord,
            AllowAdvanceEoS = input.AllowAdvanceEoS,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(separationReason);
        await ValidateUniquenessAsync(separationReason);

        await _separationReasonRepository.InsertAsync(separationReason);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditSeparationReasonDto input)
    {
        var separationReason = await _separationReasonRepository.GetAsync(input.Id.Value);

        if (separationReason.IsSystemRecord)
        {
            throw new UserFriendlyException(L("SeparationReasonIsSystemSpecific", separationReason.NameEn));
        }

        separationReason.Code = input.Code;
        separationReason.NameEn = input.NameEn;
        separationReason.NameAr = input.NameAr;
        separationReason.IsSystemRecord = input.IsSystemRecord;
        separationReason.AllowAdvanceEoS = input.AllowAdvanceEoS;

        await AssignCodeIfEmptyAsync(separationReason);
        await ValidateUniquenessAsync(separationReason);

        await _separationReasonRepository.UpdateAsync(separationReason);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_SeparationReasons_Delete)]
    public async Task Delete(EntityDto input)
    {
        var separationReason = await _separationReasonRepository.GetAsync(input.Id);

        if (separationReason.IsSystemRecord)
        {
            throw new UserFriendlyException(L("SeparationReasonIsSystemSpecific", separationReason.NameEn));
        }

        await _separationReasonRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(SeparationReason separationReason)
    {
        if (!separationReason.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _separationReasonRepository.GetAll()
            .Where(s => s.Code != null && s.Code != "")
            .Select(s => s.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        separationReason.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(SeparationReason separationReason)
    {
        var duplicateCode = await _separationReasonRepository.GetAll()
            .AnyAsync(s => s.Id != separationReason.Id && s.Code == separationReason.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _separationReasonRepository.GetAll()
            .AnyAsync(s => s.Id != separationReason.Id &&
                           (s.NameEn == separationReason.NameEn || s.NameAr == separationReason.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static SeparationReasonDto ToDto(SeparationReason separationReason)
    {
        return new SeparationReasonDto
        {
            Id = separationReason.Id,
            Code = separationReason.Code,
            NameEn = separationReason.NameEn,
            NameAr = separationReason.NameAr,
            IsSystemRecord = separationReason.IsSystemRecord,
            AllowAdvanceEoS = separationReason.AllowAdvanceEoS
        };
    }

    private static CreateOrEditSeparationReasonDto ToCreateOrEditDto(SeparationReason separationReason)
    {
        return new CreateOrEditSeparationReasonDto
        {
            Id = separationReason.Id,
            Code = separationReason.Code,
            NameEn = separationReason.NameEn,
            NameAr = separationReason.NameAr,
            IsSystemRecord = separationReason.IsSystemRecord,
            AllowAdvanceEoS = separationReason.AllowAdvanceEoS
        };
    }
}
