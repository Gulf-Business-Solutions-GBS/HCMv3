using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Dependency;
using Abp.Domain.Entities;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Linq.Extensions;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.Workflow.Dto;
using HCMv3.Workflow.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.Workflow;

/// <inheritdoc cref="IWorkflowTransactionTypesAppService"/>
[AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowTransactionTypes)]
public class WorkflowTransactionTypesAppService : HCMv3AppServiceBase, IWorkflowTransactionTypesAppService
{
    private readonly IRepository<WorkflowTransactionType> _repository;
    private readonly IRepository<WorkflowTransactionTypeRule> _ruleRepository;
    private readonly IRepository<RequestMaster> _requestMasterRepository;
    private readonly IWorkflowTransactionHandlerResolver _handlerResolver;
    private readonly IIocResolver _iocResolver;

    public WorkflowTransactionTypesAppService(
        IRepository<WorkflowTransactionType> repository,
        IRepository<WorkflowTransactionTypeRule> ruleRepository,
        IRepository<RequestMaster> requestMasterRepository,
        IWorkflowTransactionHandlerResolver handlerResolver,
        IIocResolver iocResolver)
    {
        _repository = repository;
        _ruleRepository = ruleRepository;
        _requestMasterRepository = requestMasterRepository;
        _handlerResolver = handlerResolver;
        _iocResolver = iocResolver;
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

        var rules = await _ruleRepository.GetAll()
            .Where(r => r.WorkflowTransactionTypeId == entity.Id)
            .OrderBy(r => r.Id)
            .Select(r => new WorkflowTransactionTypeRuleDto
            {
                RequestSubTypeId = r.RequestSubTypeId,
                RequestAction = r.RequestAction,
                RequestMasterId = r.RequestMasterId
            })
            .ToListAsync();

        return new GetWorkflowTransactionTypeForEditOutput
        {
            // What can be raised against an approved transaction of this type, so the dialog offers a chain
            // "for cancellations" or "for returns" only where there is such a thing. Most types have none.
            SupportedChangeActions = (_handlerResolver.Resolve(entity.Code)?.SupportedChangeActions
                                      ?? System.Array.Empty<WorkflowRequestAction>()).ToList(),
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
                AllowDirectEntry = entity.AllowDirectEntry,
                Rules = rules
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
        await ValidateRulesAsync(entity, input.Rules);

        var id = await _repository.InsertAndGetIdAsync(entity);
        await ReplaceRulesAsync(id, entity.TenantId, input.Rules);
    }

    [AbpAuthorize(AppPermissions.Pages_Workflow_WorkflowTransactionTypes_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditWorkflowTransactionTypeDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);

        MapToEntity(input, entity);
        await ValidateAsync(entity);
        await ValidateRulesAsync(entity, input.Rules);

        await _repository.UpdateAsync(entity);
        await ReplaceRulesAsync(entity.Id, entity.TenantId, input.Rules);
    }

    /// <summary>The tables a sub type can come from, for the Sub Type Source dropdown.</summary>
    public List<string> GetSubTypeSources()
    {
        return WorkflowSubTypeSources.All.ToList();
    }

    /// <summary>
    /// The rows of one sub type source -- the absence types for leave, the document types for documents --
    /// so a routing rule can name "Sick Leave" instead of an id. Empty for an unknown or empty source.
    /// </summary>
    public async Task<List<WorkflowSubTypeOptionDto>> GetSubTypeOptions(string source)
    {
        return source switch
        {
            WorkflowSubTypeSources.AbsenceType => await OptionsOf<HCMv3.PayrollManagement.AbsenceType>(),
            WorkflowSubTypeSources.AttendanceType => await OptionsOf<HCMv3.PayrollManagement.AttendanceType>(),
            WorkflowSubTypeSources.ClearanceRequestType => await OptionsOf<HCMv3.GeneralMasterInfo.ClearanceRequestType>(),
            WorkflowSubTypeSources.DisciplinaryType => await OptionsOf<HCMv3.GeneralMasterInfo.DisciplinaryType>(),
            WorkflowSubTypeSources.DocumentType => await OptionsOf<HCMv3.GeneralMasterInfo.DocumentType>(),
            WorkflowSubTypeSources.Duty => await OptionsOf<HCMv3.TimeManagement.Duty>(),
            WorkflowSubTypeSources.FormType => await OptionsOf<HCMv3.PerformanceManagement.FormType>(),
            WorkflowSubTypeSources.HRServiceType => await OptionsOf<HCMv3.GeneralMasterInfo.HRServiceType>(),
            WorkflowSubTypeSources.ITServiceType => await OptionsOf<HCMv3.GeneralMasterInfo.ITServiceType>(),
            WorkflowSubTypeSources.LoanType => await OptionsOf<HCMv3.GeneralMasterInfo.LoanType>(),
            WorkflowSubTypeSources.PersonalAction => await OptionsOf<HCMv3.GeneralMasterInfo.PersonalAction>(),
            WorkflowSubTypeSources.TransType => await OptionsOf<HCMv3.TimeManagement.TransType>(),
            WorkflowSubTypeSources.TravelType => await OptionsOf<HCMv3.TravelManagement.TravelType>(),
            _ => new List<WorkflowSubTypeOptionDto>()
        };
    }

    /// <summary>
    /// Id, Code and names of any sub type table. Every source has those columns but they share no
    /// interface, so the projection is built from the property names instead of being written thirteen times.
    /// </summary>
    private async Task<List<WorkflowSubTypeOptionDto>> OptionsOf<T>() where T : class, IEntity<int>
    {
        var repository = _iocResolver.Resolve<IRepository<T>>();

        try
        {
            var row = Expression.Parameter(typeof(T), "row");
            var dto = typeof(WorkflowSubTypeOptionDto);
            var projection = Expression.Lambda<Func<T, WorkflowSubTypeOptionDto>>(
                Expression.MemberInit(
                    Expression.New(dto),
                    Expression.Bind(dto.GetProperty(nameof(WorkflowSubTypeOptionDto.Id))!, Expression.Property(row, "Id")),
                    Expression.Bind(dto.GetProperty(nameof(WorkflowSubTypeOptionDto.Code))!, Expression.Property(row, "Code")),
                    Expression.Bind(dto.GetProperty(nameof(WorkflowSubTypeOptionDto.NameEn))!, Expression.Property(row, "NameEn")),
                    Expression.Bind(dto.GetProperty(nameof(WorkflowSubTypeOptionDto.NameAr))!, Expression.Property(row, "NameAr"))),
                row);

            return await repository.GetAll()
                .Select(projection)
                .OrderBy(o => o.NameEn)
                .ToListAsync();
        }
        finally
        {
            _iocResolver.Release(repository);
        }
    }

    /// <summary>
    /// Every rule must name a chain that exists, may name a sub type only when the type has a sub type
    /// source, and no two rules may cover the same sub type and action -- which of them won would be an
    /// accident of row order.
    /// </summary>
    private async Task ValidateRulesAsync(WorkflowTransactionType entity, List<WorkflowTransactionTypeRuleDto> rules)
    {
        rules ??= new List<WorkflowTransactionTypeRuleDto>();

        if (rules.GroupBy(r => new { r.RequestSubTypeId, r.RequestAction }).Any(g => g.Count() > 1))
        {
            throw new UserFriendlyException(L("WorkflowTransactionTypeRuleDuplicate"));
        }

        if (entity.SubTypeSource.IsNullOrWhiteSpace() && rules.Any(r => r.RequestSubTypeId.HasValue))
        {
            throw new UserFriendlyException(L("WorkflowTransactionTypeRuleNeedsSubTypeSource"));
        }

        var chainIds = rules.Select(r => r.RequestMasterId).Distinct().ToList();
        var found = await _requestMasterRepository.GetAll().CountAsync(m => chainIds.Contains(m.Id));
        if (found != chainIds.Count)
        {
            throw new UserFriendlyException(L("NoWorkflowConfigurationFoundForRequestType", entity.NameEn));
        }
    }

    /// <summary>The list sent replaces the rules the type had.</summary>
    private async Task ReplaceRulesAsync(int transactionTypeId, int tenantId, List<WorkflowTransactionTypeRuleDto> rules)
    {
        await _ruleRepository.DeleteAsync(r => r.WorkflowTransactionTypeId == transactionTypeId);

        foreach (var rule in rules ?? new List<WorkflowTransactionTypeRuleDto>())
        {
            await _ruleRepository.InsertAsync(new WorkflowTransactionTypeRule
            {
                TenantId = tenantId,
                WorkflowTransactionTypeId = transactionTypeId,
                RequestSubTypeId = rule.RequestSubTypeId,
                RequestAction = rule.RequestAction,
                RequestMasterId = rule.RequestMasterId
            });
        }
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
