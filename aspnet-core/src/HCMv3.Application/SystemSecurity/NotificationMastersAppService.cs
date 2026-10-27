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
using HCMv3.HcmNotifications.Rules;
using HCMv3.SystemSecurity.Dto;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.SystemSecurity;

/// <summary>
/// Application service for <see cref="NotificationMaster"/> (ported from legacy XAF
/// HCM10.Module.BusinessObjects.SysSecurity.NotificationMaster -- see entity remarks).
///
/// <para>
/// VALIDATION. The type must be a supported rule source, the sub type one of that source's, and the criterion
/// must parse and name only fields the source exposes -- checked here so an administrator learns about a typo
/// on save rather than from a rule that silently never fires.
/// </para>
///
/// <para>
/// DELIBERATE CHANGE FROM LEGACY: legacy allowed one rule per NotificationType. That limit existed because
/// every rule on a table shared the record's single <c>NotificationSent</c> flag, so a second rule could never
/// fire anyway. With the per-rule sent log, two rules on the same type now both work, so only an exact
/// duplicate (same type, sub type, criterion and configuration) is refused.
/// </para>
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters)]
public class NotificationMastersAppService : HCMv3AppServiceBase, INotificationMastersAppService
{
    private readonly IRepository<NotificationMaster> _repository;
    private readonly INotificationRuleSourceRegistry _sourceRegistry;
    private readonly INotificationCriterionEvaluator _criterionEvaluator;

    public NotificationMastersAppService(
        IRepository<NotificationMaster> repository,
        INotificationRuleSourceRegistry sourceRegistry,
        INotificationCriterionEvaluator criterionEvaluator)
    {
        _repository = repository;
        _sourceRegistry = sourceRegistry;
        _criterionEvaluator = criterionEvaluator;
    }

    [HttpPost]
    public async Task<PagedResultDto<NotificationMasterDto>> GetAll(GetAllNotificationMastersInput input)
    {
        input.Normalize();

        var query = _repository.GetAll()
            .Include(m => m.NotificationConfig)
            .WhereIf(!input.Filter.IsNullOrWhiteSpace(), m => m.NotificationType.Contains(input.Filter));

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        var subTypeNames = new Dictionary<string, Dictionary<int, string>>();
        var dtos = new List<NotificationMasterDto>();

        foreach (var item in items)
        {
            dtos.Add(await ToDtoAsync(item, subTypeNames));
        }

        return new PagedResultDto<NotificationMasterDto>(totalCount, dtos);
    }

    public async Task<GetNotificationMasterForEditOutput> GetNotificationMasterForEdit(EntityDto input)
    {
        var entity = await _repository.GetAsync(input.Id);

        return new GetNotificationMasterForEditOutput
        {
            NotificationMaster = ToCreateOrEditDto(entity)
        };
    }

    public async Task CreateOrEdit(CreateOrEditNotificationMasterDto input)
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

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Delete)]
    public async Task Delete(EntityDto input)
    {
        await _repository.DeleteAsync(input.Id);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Create)]
    protected virtual async Task CreateAsync(CreateOrEditNotificationMasterDto input)
    {
        var entity = MapToEntity(input, new NotificationMaster());
        entity.TenantId = AbpSession.TenantId ?? 0;

        await ValidateAsync(entity);

        await _repository.InsertAsync(entity);
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Edit)]
    protected virtual async Task UpdateAsync(CreateOrEditNotificationMasterDto input)
    {
        var entity = await _repository.GetAsync(input.Id.Value);
        entity = MapToEntity(input, entity);

        await ValidateAsync(entity);

        await _repository.UpdateAsync(entity);
    }

    private async Task ValidateAsync(NotificationMaster entity)
    {
        var source = _sourceRegistry.Find(entity.NotificationType);
        if (source == null)
        {
            throw new UserFriendlyException(L("NotificationTypeNotSupported", entity.NotificationType));
        }

        // Stored as the source's own code, so "employeedocument" typed by hand still runs.
        entity.NotificationType = source.Code;

        if (entity.NotificationSubTypeId.HasValue)
        {
            var subTypes = source.SubTypeDisplayNameKey == null
                ? new List<NotificationRuleSubTypeItem>()
                : await source.GetSubTypesAsync();

            if (subTypes.All(s => s.Id != entity.NotificationSubTypeId.Value))
            {
                throw new UserFriendlyException(L("NotificationSubTypeNotValid"));
            }
        }

        var criterionErrors = _criterionEvaluator.Validate(source, entity.Criterion);
        if (criterionErrors.Count > 0)
        {
            throw new UserFriendlyException(L("NotificationCriterionNotValid"), string.Join(" ", criterionErrors));
        }

        var duplicate = await _repository.GetAll()
            .AnyAsync(m => m.Id != entity.Id
                           && m.NotificationType == entity.NotificationType
                           && m.NotificationSubTypeId == entity.NotificationSubTypeId
                           && m.NotificationConfigId == entity.NotificationConfigId
                           && m.Criterion == entity.Criterion);
        if (duplicate)
        {
            throw new UserFriendlyException(L("NotificationTypeAlreadyAssigned"));
        }
    }

    private static NotificationMaster MapToEntity(CreateOrEditNotificationMasterDto input, NotificationMaster entity)
    {
        entity.NotificationType = input.NotificationType?.Trim();
        entity.NotificationSubTypeId = input.NotificationSubTypeId;
        entity.NotificationConfigId = input.NotificationConfigId;
        entity.Criterion = input.Criterion.IsNullOrWhiteSpace() ? null : input.Criterion;
        entity.IsEnabled = input.IsEnabled;

        return entity;
    }

    private async Task<NotificationMasterDto> ToDtoAsync(
        NotificationMaster entity,
        Dictionary<string, Dictionary<int, string>> subTypeNamesBySource)
    {
        var source = _sourceRegistry.Find(entity.NotificationType);
        string subTypeName = null;

        if (source != null && source.SubTypeDisplayNameKey != null && entity.NotificationSubTypeId.HasValue)
        {
            if (!subTypeNamesBySource.TryGetValue(source.Code, out var names))
            {
                names = (await source.GetSubTypesAsync()).ToDictionary(s => s.Id, s => s.Name);
                subTypeNamesBySource[source.Code] = names;
            }

            names.TryGetValue(entity.NotificationSubTypeId.Value, out subTypeName);
        }

        return new NotificationMasterDto
        {
            Id = entity.Id,
            NotificationType = entity.NotificationType,
            NotificationTypeName = source == null ? entity.NotificationType : L(source.DisplayNameKey),
            IsSupportedType = source != null,
            NotificationSubTypeId = entity.NotificationSubTypeId,
            NotificationSubTypeName = subTypeName,
            NotificationConfigId = entity.NotificationConfigId,
            NotificationConfigName = entity.NotificationConfig?.Name,
            Criterion = entity.Criterion,
            IsEnabled = entity.IsEnabled
        };
    }

    private static CreateOrEditNotificationMasterDto ToCreateOrEditDto(NotificationMaster entity)
    {
        return new CreateOrEditNotificationMasterDto
        {
            Id = entity.Id,
            NotificationType = entity.NotificationType,
            NotificationSubTypeId = entity.NotificationSubTypeId,
            NotificationConfigId = entity.NotificationConfigId,
            Criterion = entity.Criterion,
            IsEnabled = entity.IsEnabled
        };
    }
}
