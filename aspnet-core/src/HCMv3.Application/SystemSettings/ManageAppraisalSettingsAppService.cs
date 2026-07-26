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
/// Application service for <see cref="ManageAppraisalSettings"/>. Legacy <c>OnDeleting</c> unconditionally
/// throws ("You can't delete record. Please contact administrator."), so <see cref="DeleteAppraisalSettings"/>
/// always throws here too. Legacy <c>PerformValidation</c> was empty (no real business rule to replicate).
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_AppraisalSettings)]
public class ManageAppraisalSettingsAppService : HCMv3AppServiceBase, IManageAppraisalSettingsAppService
{
    private readonly IRepository<ManageAppraisalSettings> _repository;

    public ManageAppraisalSettingsAppService(IRepository<ManageAppraisalSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<AppraisalSettingsListDto>> GetAppraisalSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<AppraisalSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<AppraisalSettingsEditDto> GetAppraisalSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    public async Task CreateOrEditAppraisalSettings(AppraisalSettingsEditDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_AppraisalSettings_Delete)]
    public Task DeleteAppraisalSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_AppraisalSettings_Create)]
    protected virtual async Task CreateAsync(AppraisalSettingsEditDto input)
    {
        var entity = new ManageAppraisalSettings
        {
            AppraisalCycle = input.AppraisalCycle,
            EnablePAPeerReview = input.EnablePAPeerReview,
            SetPeerMinLimit = input.SetPeerMinLimit,
            SetPeerMaxLimit = input.SetPeerMaxLimit,
            EnableMarksInPAReview = input.EnableMarksInPAReview,
            MarksOn = input.MarksOn,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_AppraisalSettings_Edit)]
    protected virtual async Task UpdateAsync(AppraisalSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        entity.AppraisalCycle = input.AppraisalCycle;
        entity.EnablePAPeerReview = input.EnablePAPeerReview;
        entity.SetPeerMinLimit = input.SetPeerMinLimit;
        entity.SetPeerMaxLimit = input.SetPeerMaxLimit;
        entity.EnableMarksInPAReview = input.EnableMarksInPAReview;
        entity.MarksOn = input.MarksOn;

        await _repository.UpdateAsync(entity);
    }

    private static AppraisalSettingsListDto ToListDto(ManageAppraisalSettings entity)
    {
        return new AppraisalSettingsListDto
        {
            Id = entity.Id,
            AppraisalCycle = entity.AppraisalCycle,
            EnablePAPeerReview = entity.EnablePAPeerReview,
            SetPeerMinLimit = entity.SetPeerMinLimit,
            SetPeerMaxLimit = entity.SetPeerMaxLimit,
            EnableMarksInPAReview = entity.EnableMarksInPAReview,
            MarksOn = entity.MarksOn
        };
    }

    private static AppraisalSettingsEditDto ToEditDto(ManageAppraisalSettings entity)
    {
        return new AppraisalSettingsEditDto
        {
            Id = entity.Id,
            AppraisalCycle = entity.AppraisalCycle,
            EnablePAPeerReview = entity.EnablePAPeerReview,
            SetPeerMinLimit = entity.SetPeerMinLimit,
            SetPeerMaxLimit = entity.SetPeerMaxLimit,
            EnableMarksInPAReview = entity.EnableMarksInPAReview,
            MarksOn = entity.MarksOn
        };
    }
}
