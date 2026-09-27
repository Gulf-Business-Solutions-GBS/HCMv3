using System.Collections.Generic;
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
using HCMv3.Workflow.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <inheritdoc cref="IWorkflowTransactionTypesAppService"/>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowTransactionTypes)]
public class WorkflowTransactionTypesAppService : HCMv3AppServiceBase, IWorkflowTransactionTypesAppService
{
    private readonly IRepository<WorkflowTransactionType> _repository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;

    public WorkflowTransactionTypesAppService(
        IRepository<WorkflowTransactionType> repository,
        IRepository<RequestMaster> requestMasterRepository)
    {
        _repository = repository;
        _requestMasterRepository = requestMasterRepository;
    }

    [HttpPost]
    public async Task<PagedResultDto<WorkflowTransactionTypeDto>> GetAll(GetAllWorkflowTransactionTypesInput input)
    {
        input.Normalize();

        var filter = input.Filter;

        var query = _repository.GetAll()
            .Include(t => t.DefaultRequestMaster)
            .WhereIf(!filter.IsNullOrWhiteSpace(), t =>
                t.Code.Contains(filter) || t.NameEn.Contains(filter) || t.NameAr.Contains(filter))
            .WhereIf(input.IsWorkflowEnabled.HasValue, t => t.IsWorkflowEnabled == input.IsWorkflowEnabled.Value)
            .WhereIf(input.IsEssEnabled.HasValue, t => t.IsEssEnabled == input.IsEssEnabled.Value)
            .WhereIf(input.IsActive.HasValue, t => t.IsActive == input.IsActive.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<WorkflowTransactionTypeDto>(totalCount, items.Select(ToDto).ToList());
    }

    public async Task<GetWorkflowTransactionTypeForEditOutput> GetWorkflowTransactionTypeForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetWorkflowTransactionTypeForEditOutput
        {
            WorkflowTransactionType = new CreateOrEditWorkflowTransactionTypeDto
            {
                Id = entity.Id,
                Code = entity.Code,
                NameEn = entity.NameEn,
                NameAr = entity.NameAr,
                IsWorkflowEnabled = entity.IsWorkflowEnabled,
                IsEssEnabled = entity.IsEssEnabled,
                DefaultRequestMasterId = entity.DefaultRequestMasterId,
                EssRoute = entity.EssRoute,
                EssPermissionName = entity.EssPermissionName,
                SubTypeSource = entity.SubTypeSource,
                IconClass = entity.IconClass,
                DisplayOrder = entity.DisplayOrder,
                IsActive = entity.IsActive,
                AllowDirectEntry = entity.AllowDirectEntry
            }
        };
    }

    public async Task CreateOrEdit(CreateOrEditWorkflowTransactionTypeDto input)
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

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Create)]
    protected virtual async Task CreateAsync(CreateOrEditWorkflowTransactionTypeDto input)
    {
        var entity = new WorkflowTransactionType { TenantId = AbpSession.TenantId ?? 0 };

        MapToEntity(input, entity);
        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowTransactionTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        MapToEntity(input, entity);
        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    public async Task<List<WorkflowTransactionTypeDto>> GetEssEnabledTypes()
    {
        var items = await _repository.GetAll()
            .Include(t => t.DefaultRequestMaster)
            .Where(t => t.IsEssEnabled && t.IsActive)
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Code)
            .ToListAsync();

        return items.Select(ToDto).ToList();
    }

    private async Task ValidateAsync(WorkflowTransactionType entity)
    {
        // Code is the join key between this catalog, WorkflowRequest.RequestType and
        // WorkflowGroupDetail.RequestTypeName. A duplicate makes routing ambiguous -- precisely the failure
        // this catalog exists to remove -- so it is rejected here as well as by the unique index.
        var duplicate = await _repository.GetAll()
            .AnyAsync(t => t.Id != entity.Id && t.Code == entity.Code);

        if (duplicate)
        {
            throw new UserFriendlyException(L("DuplicateCodeFound"));
        }

        if (entity.DefaultRequestMasterId.HasValue)
        {
            var masterExists = await _requestMasterRepository.GetAll()
                .AnyAsync(m => m.Id == entity.DefaultRequestMasterId.Value);

            if (!masterExists)
            {
                throw new UserFriendlyException(L("NoWorkflowConfigurationFoundForRequestType", entity.NameEn));
            }
        }

        // Enabling workflow with nowhere to route to would let a submit fail only at run time, for the
        // employee, rather than here for the admin who can fix it.
        if (entity.IsWorkflowEnabled && !entity.DefaultRequestMasterId.HasValue)
        {
            throw new UserFriendlyException(L("WorkflowEnabledTypeNeedsADefaultRequestMaster", entity.NameEn));
        }

        // Some types are routed for approval but are NOT self-service -- a personnel action is raised by HR,
        // never by the employee it concerns, so it has no ESS route at all. Switching ESS on for one of
        // those would put a tile on the landing page leading to a 404, which an admin would discover only
        // after employees started clicking it.
        if (entity.IsEssEnabled && entity.EssRoute.IsNullOrWhiteSpace())
        {
            throw new UserFriendlyException(L("EssEnabledTypeNeedsARoute", entity.NameEn));
        }
    }

    private static void MapToEntity(CreateOrEditWorkflowTransactionTypeDto input, WorkflowTransactionType entity)
    {
        entity.Code = input.Code?.Trim();
        entity.NameEn = input.NameEn;
        entity.NameAr = input.NameAr;
        entity.IsWorkflowEnabled = input.IsWorkflowEnabled;
        entity.IsEssEnabled = input.IsEssEnabled;
        entity.DefaultRequestMasterId = input.DefaultRequestMasterId;
        entity.EssRoute = input.EssRoute;
        entity.EssPermissionName = input.EssPermissionName;
        entity.SubTypeSource = input.SubTypeSource;
        entity.IconClass = input.IconClass;
        entity.DisplayOrder = input.DisplayOrder;
        entity.IsActive = input.IsActive;
        entity.AllowDirectEntry = input.AllowDirectEntry;
    }

    private static WorkflowTransactionTypeDto ToDto(WorkflowTransactionType entity)
    {
        return new WorkflowTransactionTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            NameEn = entity.NameEn,
            NameAr = entity.NameAr,
            IsWorkflowEnabled = entity.IsWorkflowEnabled,
            IsEssEnabled = entity.IsEssEnabled,
            DefaultRequestMasterId = entity.DefaultRequestMasterId,
            DefaultRequestMasterNameEn = entity.DefaultRequestMaster?.NameEn,
            DefaultRequestMasterSteps = entity.DefaultRequestMaster?.WorkflowSteps,
            EssRoute = entity.EssRoute,
            EssPermissionName = entity.EssPermissionName,
            SubTypeSource = entity.SubTypeSource,
            IconClass = entity.IconClass,
            DisplayOrder = entity.DisplayOrder,
            IsActive = entity.IsActive,
            AllowDirectEntry = entity.AllowDirectEntry
        };
    }
}
