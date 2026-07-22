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
/// Application service for the PersonalAction master (ported from legacy XAF PersonalAction business object).
/// The legacy <c>PerformValidation</c> logic (auto Code, duplicate Code/Name checks) lives here,
/// out of the persistence lifecycle.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActions)]
public class PersonalActionsAppService : HCMv3AppServiceBase, IPersonalActionsAppService
{
    private readonly IRepository<PersonalAction> _personalActionRepository;

    public PersonalActionsAppService(IRepository<PersonalAction> personalActionRepository)
    {
        _personalActionRepository = personalActionRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<PersonalActionDto>> GetAll(GetAllPersonalActionsInput input)
    {
        input.Normalize();

        var query = _personalActionRepository.GetAll()
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), p =>
                p.Code.Contains(input.Filter) ||
                p.NameEn.Contains(input.Filter) ||
                p.NameAr.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<PersonalActionDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetPersonalActionForEditOutput> GetPersonalActionForEdit(EntityDto input)
    {
        var personalAction = await _personalActionRepository.GetAsync(input.Id);

        return new GetPersonalActionForEditOutput
        {
            PersonalAction = ToCreateOrEditDto(personalAction)
        };
    }

    public async Task CreateOrEdit(CreateOrEditPersonalActionDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActions_Create)]
    protected virtual async Task CreateAsync(CreateOrEditPersonalActionDto input)
    {
        var personalAction = new PersonalAction
        {
            Code = input.Code,
            NameEn = input.NameEn,
            NameAr = input.NameAr,
            ReqOrgAssignment = input.ReqOrgAssignment,
            ReqBasicPay = input.ReqBasicPay,
            ReqContract = input.ReqContract,
            ReqManager = input.ReqManager,
            ReqOnHold = input.ReqOnHold,
            ReqSeparation = input.ReqSeparation,
            ReqFnFPayment = input.ReqFnFPayment,
            IncludeInWorkflow = input.IncludeInWorkflow,
            IsAttachmentRequired = input.IsAttachmentRequired,
            TenantId = AbpSession.TenantId ?? 0
        };

        await AssignCodeIfEmptyAsync(personalAction);
        await ValidateUniquenessAsync(personalAction);

        await _personalActionRepository.InsertAsync(personalAction);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActions_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditPersonalActionDto input)
    {
        var personalAction = await _personalActionRepository.GetAsync(input.Id.Value);

        personalAction.Code = input.Code;
        personalAction.NameEn = input.NameEn;
        personalAction.NameAr = input.NameAr;
        personalAction.ReqOrgAssignment = input.ReqOrgAssignment;
        personalAction.ReqBasicPay = input.ReqBasicPay;
        personalAction.ReqContract = input.ReqContract;
        personalAction.ReqManager = input.ReqManager;
        personalAction.ReqOnHold = input.ReqOnHold;
        personalAction.ReqSeparation = input.ReqSeparation;
        personalAction.ReqFnFPayment = input.ReqFnFPayment;
        personalAction.IncludeInWorkflow = input.IncludeInWorkflow;
        personalAction.IsAttachmentRequired = input.IsAttachmentRequired;

        await AssignCodeIfEmptyAsync(personalAction);
        await ValidateUniquenessAsync(personalAction);

        await _personalActionRepository.UpdateAsync(personalAction);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_PersonalActions_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _personalActionRepository.DeleteAsync(input.Id);
    }

    /// <summary>
    /// Legacy <c>GetMaxCode</c> parity: when Code is blank, assign (max existing numeric code) + 1.
    /// </summary>
    private async Task AssignCodeIfEmptyAsync(PersonalAction personalAction)
    {
        if (!personalAction.Code.IsNullOrWhiteSpace())
        {
            return;
        }

        var numericCodes = await _personalActionRepository.GetAll()
            .Where(p => p.Code != null && p.Code != "")
            .Select(p => p.Code)
            .ToListAsync();

        var maxCode = numericCodes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        personalAction.Code = (maxCode + 1).ToString();
    }

    /// <summary>
    /// Legacy <c>PerformValidation</c> parity: unique Code and unique NameEn/NameAr.
    /// </summary>
    private async Task ValidateUniquenessAsync(PersonalAction personalAction)
    {
        var duplicateCode = await _personalActionRepository.GetAll()
            .AnyAsync(p => p.Id != personalAction.Id && p.Code == personalAction.Code);
        if (duplicateCode)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        var duplicateName = await _personalActionRepository.GetAll()
            .AnyAsync(p => p.Id != personalAction.Id &&
                           (p.NameEn == personalAction.NameEn || p.NameAr == personalAction.NameAr));
        if (duplicateName)
        {
            throw new UserFriendlyException(L("NameAlreadyInUse"));
        }
    }

    private static PersonalActionDto ToDto(PersonalAction personalAction)
    {
        return new PersonalActionDto
        {
            Id = personalAction.Id,
            Code = personalAction.Code,
            NameEn = personalAction.NameEn,
            NameAr = personalAction.NameAr,
            ReqOrgAssignment = personalAction.ReqOrgAssignment,
            ReqBasicPay = personalAction.ReqBasicPay,
            ReqContract = personalAction.ReqContract,
            ReqManager = personalAction.ReqManager,
            ReqOnHold = personalAction.ReqOnHold,
            ReqSeparation = personalAction.ReqSeparation,
            ReqFnFPayment = personalAction.ReqFnFPayment,
            IncludeInWorkflow = personalAction.IncludeInWorkflow,
            IsAttachmentRequired = personalAction.IsAttachmentRequired
        };
    }

    private static CreateOrEditPersonalActionDto ToCreateOrEditDto(PersonalAction personalAction)
    {
        return new CreateOrEditPersonalActionDto
        {
            Id = personalAction.Id,
            Code = personalAction.Code,
            NameEn = personalAction.NameEn,
            NameAr = personalAction.NameAr,
            ReqOrgAssignment = personalAction.ReqOrgAssignment,
            ReqBasicPay = personalAction.ReqBasicPay,
            ReqContract = personalAction.ReqContract,
            ReqManager = personalAction.ReqManager,
            ReqOnHold = personalAction.ReqOnHold,
            ReqSeparation = personalAction.ReqSeparation,
            ReqFnFPayment = personalAction.ReqFnFPayment,
            IncludeInWorkflow = personalAction.IncludeInWorkflow,
            IsAttachmentRequired = personalAction.IsAttachmentRequired
        };
    }
}
