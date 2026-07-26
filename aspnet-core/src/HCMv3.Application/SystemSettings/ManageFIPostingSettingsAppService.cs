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
using HCMv3.SystemSettings.Dto;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSettings;

/// <summary>
/// Application service for <see cref="ManageFIPostingSettings"/>. Legacy <c>OnDeleting</c> unconditionally
/// throws ("You can't delete record. Please contact administrator."), so
/// <see cref="DeleteFIPostingSettings"/> always throws here too. Legacy <c>PerformValidation</c> was empty
/// (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_FIPostingSettings)]
public class ManageFIPostingSettingsAppService : HCMv3AppServiceBase, IManageFIPostingSettingsAppService
{
    private readonly IRepository<ManageFIPostingSettings> _repository;

    public ManageFIPostingSettingsAppService(IRepository<ManageFIPostingSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<FIPostingSettingsListDto>> GetFIPostingSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<FIPostingSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<FIPostingSettingsEditDto> GetFIPostingSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    public async Task CreateOrEditFIPostingSettings(FIPostingSettingsEditDto input)
    {
        if (input.Id != 0)
        {
            await UpdateAsync(input);
        }
        else
        {
            await CreateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_FIPostingSettings_Delete)]
    public Task DeleteFIPostingSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_FIPostingSettings_Create)]
    protected virtual async Task CreateAsync(FIPostingSettingsEditDto input)
    {
        var entity = new ManageFIPostingSettings
        {
            SeparateProvisionPosting = input.SeparateProvisionPosting,
            EnableAutoWageAccountEntry = input.EnableAutoWageAccountEntry,
            AllowDuplicateCodeInCostElement = input.AllowDuplicateCodeInCostElement,
            FiPostingAfterPayrollExit = input.FiPostingAfterPayrollExit,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_FIPostingSettings_Edit)]
    protected virtual async Task UpdateAsync(FIPostingSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        entity.SeparateProvisionPosting = input.SeparateProvisionPosting;
        entity.EnableAutoWageAccountEntry = input.EnableAutoWageAccountEntry;
        entity.AllowDuplicateCodeInCostElement = input.AllowDuplicateCodeInCostElement;
        entity.FiPostingAfterPayrollExit = input.FiPostingAfterPayrollExit;

        await _repository.UpdateAsync(entity);
    }

    private static FIPostingSettingsListDto ToListDto(ManageFIPostingSettings entity)
    {
        return new FIPostingSettingsListDto
        {
            Id = entity.Id,
            SeparateProvisionPosting = entity.SeparateProvisionPosting,
            EnableAutoWageAccountEntry = entity.EnableAutoWageAccountEntry,
            AllowDuplicateCodeInCostElement = entity.AllowDuplicateCodeInCostElement,
            FiPostingAfterPayrollExit = entity.FiPostingAfterPayrollExit
        };
    }

    private static FIPostingSettingsEditDto ToEditDto(ManageFIPostingSettings entity)
    {
        return new FIPostingSettingsEditDto
        {
            Id = entity.Id,
            SeparateProvisionPosting = entity.SeparateProvisionPosting,
            EnableAutoWageAccountEntry = entity.EnableAutoWageAccountEntry,
            AllowDuplicateCodeInCostElement = entity.AllowDuplicateCodeInCostElement,
            FiPostingAfterPayrollExit = entity.FiPostingAfterPayrollExit
        };
    }
}
