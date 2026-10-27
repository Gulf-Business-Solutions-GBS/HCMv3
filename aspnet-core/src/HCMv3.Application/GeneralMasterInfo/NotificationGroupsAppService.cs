using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using HCMv3.Authorization;
using HCMv3.GeneralMasterInfo.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.GeneralMasterInfo;

/// <summary>
/// Application service for the NotificationGroup detail record (ported from legacy XAF NotificationGroup).
/// Pure detail record (no Code/NameEn/NameAr): plain CRUD without auto-code or duplicate validation.
/// A group names a workflow role whose current holders are notified, optionally narrowed to the requester's own
/// company, department, branch or cost centre.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups)]
public class NotificationGroupsAppService : HCMv3AppServiceBase, INotificationGroupsAppService
{
    private readonly IRepository<NotificationGroup> _repository;

    public NotificationGroupsAppService(IRepository<NotificationGroup> repository)
    {
        _repository = repository;
    }

    [HttpPost]
    public async Task<PagedResultDto<NotificationGroupDto>> GetAll(GetAllNotificationGroupsInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(g => g.NotificationConfig)
            .Include(g => g.WorkflowRole);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NotificationGroupDto>(
            totalCount,
            items.Select(ToDto).ToList());
    }

    public async Task<GetNotificationGroupForEditOutput> GetNotificationGroupForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetNotificationGroupForEditOutput
        {
            NotificationGroup = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNotificationGroupDto input)
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

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNotificationGroupDto input)
    {
        var entity = new NotificationGroup
        {
            NotificationConfigId = input.NotificationConfigId,
            WorkflowRoleId = input.WorkflowRoleId,
            WorkflowRoleGroup = input.WorkflowRoleGroup,
            WorkflowRoleOrgId = input.WorkflowRoleOrgId,
            TenantId = AbpSession.TenantId ?? 0
        };

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNotificationGroupDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        entity.NotificationConfigId = input.NotificationConfigId;
        entity.WorkflowRoleId = input.WorkflowRoleId;
        entity.WorkflowRoleGroup = input.WorkflowRoleGroup;
        entity.WorkflowRoleOrgId = input.WorkflowRoleOrgId;

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_GeneralMasterInfo_NotificationGroups_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    private static NotificationGroupDto ToDto(NotificationGroup entity)
    {
        return new NotificationGroupDto
        {
            Id = entity.Id,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleNameEn = entity.WorkflowRole?.NameEn,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId
        };
    }

    private static CreateOrEditNotificationGroupDto ToCreateOrEditDto(NotificationGroup entity)
    {
        return new CreateOrEditNotificationGroupDto
        {
            Id = entity.Id,
            NotificationConfigId = entity.NotificationConfigId,
            WorkflowRoleId = entity.WorkflowRoleId,
            WorkflowRoleGroup = entity.WorkflowRoleGroup,
            WorkflowRoleOrgId = entity.WorkflowRoleOrgId
        };
    }
}
