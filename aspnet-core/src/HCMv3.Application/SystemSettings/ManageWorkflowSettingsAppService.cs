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
/// Application service for <see cref="ManageWorkflowSettings"/> (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.SysSettings.ManageWorkflowSettings</c>). Legacy <c>OnDeleting</c>
/// unconditionally throws -- ported to <see cref="DeleteWorkflowSettings"/>.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSettings_WorkflowSettings)]
public class ManageWorkflowSettingsAppService : HCMv3AppServiceBase, IManageWorkflowSettingsAppService
{
    private readonly IRepository<ManageWorkflowSettings> _repository;

    public ManageWorkflowSettingsAppService(IRepository<ManageWorkflowSettings> repository)
    {
        _repository = repository;
    }

    public async Task<PagedResultDto<WorkflowSettingsListDto>> GetWorkflowSettingsList(PagedAndSortedResultRequestDto input)
    {
        var query = _repository.GetAll();

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting.IsNullOrWhiteSpace() ? "Id" : input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowSettingsListDto>(
            totalCount,
            items.Select(ToListDto).ToList());
    }

    public async Task<WorkflowSettingsEditDto> GetWorkflowSettingsForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        return ToEditDto(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_WorkflowSettings_Create, AppPermissions.Pages_SystemSettings_WorkflowSettings_Edit)]
    public async Task CreateOrEditWorkflowSettings(WorkflowSettingsEditDto input)
    {
        if (input.Id == 0)
        {
            await CreateAsync(input);
        }
        else
        {
            await UpdateAsync(input);
        }
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSettings_WorkflowSettings_Delete)]
    public Task DeleteWorkflowSettings(EntityDto input)
    {
        throw new UserFriendlyException(L("CannotDeleteSystemSettingsRecord"));
    }

    private async Task CreateAsync(WorkflowSettingsEditDto input)
    {
        var entity = MapToEntity(input, new ManageWorkflowSettings());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await _repository.InsertAsync(entity);
    }

    private async Task UpdateAsync(WorkflowSettingsEditDto input)
    {
        var entity = await _repository.GetAsync(input.Id);
        MapToEntity(input, entity);

        await _repository.UpdateAsync(entity);
    }

    private static ManageWorkflowSettings MapToEntity(WorkflowSettingsEditDto input, ManageWorkflowSettings entity)
    {
        entity.EnableWorkflowGroup = input.EnableWorkflowGroup;
        entity.EnableOrgUnitTypeInWorkflow = input.EnableOrgUnitTypeInWorkflow;
        entity.HideLeavePayDetailsInWorkflow = input.HideLeavePayDetailsInWorkflow;
        entity.EnableTravelCostOnWorkflowstep = input.EnableTravelCostOnWorkflowstep;
        entity.OvertimeRequestModificationStep = input.OvertimeRequestModificationStep;
        entity.DynamicWorkflowSteps = input.DynamicWorkflowSteps;
        entity.SendBackToCreator = input.SendBackToCreator;
        entity.DelegateEmployeeListByOrganizationUnitTypeId = input.DelegateEmployeeListByOrganizationUnitTypeId;

        return entity;
    }

    private static WorkflowSettingsListDto ToListDto(ManageWorkflowSettings entity)
    {
        return new WorkflowSettingsListDto
        {
            Id = entity.Id,
            EnableWorkflowGroup = entity.EnableWorkflowGroup,
            EnableOrgUnitTypeInWorkflow = entity.EnableOrgUnitTypeInWorkflow,
            HideLeavePayDetailsInWorkflow = entity.HideLeavePayDetailsInWorkflow,
            EnableTravelCostOnWorkflowstep = entity.EnableTravelCostOnWorkflowstep,
            OvertimeRequestModificationStep = entity.OvertimeRequestModificationStep,
            DynamicWorkflowSteps = entity.DynamicWorkflowSteps,
            SendBackToCreator = entity.SendBackToCreator,
            DelegateEmployeeListByOrganizationUnitTypeId = entity.DelegateEmployeeListByOrganizationUnitTypeId
        };
    }

    private static WorkflowSettingsEditDto ToEditDto(ManageWorkflowSettings entity)
    {
        return new WorkflowSettingsEditDto
        {
            Id = entity.Id,
            EnableWorkflowGroup = entity.EnableWorkflowGroup,
            EnableOrgUnitTypeInWorkflow = entity.EnableOrgUnitTypeInWorkflow,
            HideLeavePayDetailsInWorkflow = entity.HideLeavePayDetailsInWorkflow,
            EnableTravelCostOnWorkflowstep = entity.EnableTravelCostOnWorkflowstep,
            OvertimeRequestModificationStep = entity.OvertimeRequestModificationStep,
            DynamicWorkflowSteps = entity.DynamicWorkflowSteps,
            SendBackToCreator = entity.SendBackToCreator,
            DelegateEmployeeListByOrganizationUnitTypeId = entity.DelegateEmployeeListByOrganizationUnitTypeId
        };
    }
}
