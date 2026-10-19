using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Linq.Extensions;
using Abp.Timing;
using Abp.UI;
using HCMv3.Authorization;
using HCMv3.BusinessRules;
using HCMv3.BusinessRules.Dto;
using HCMv3.BusinessRules.Services;
using HCMv3.HcmNotifications.Dto;
using HCMv3.HcmNotifications.Rules;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HCMv3.HcmNotifications;

/// <summary>
/// The rule-engine half of the Notification Master screen. Guarded by the screen's own permissions, so no new
/// permission is needed: anyone who can see the rules can see what they would match and what they sent, and
/// marking records as sent -- which changes what the next run does -- needs Edit.
/// </summary>
[AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters)]
public class NotificationRulesAppService : HCMv3AppServiceBase, INotificationRulesAppService
{
    private readonly INotificationRuleSourceRegistry _sourceRegistry;
    private readonly IRuleFunctionRegistry _functionRegistry;
    private readonly INotificationRuleRunner _runner;
    private readonly IRepository<NotificationSentLog, long> _sentLogRepository;

    public NotificationRulesAppService(
        INotificationRuleSourceRegistry sourceRegistry,
        IRuleFunctionRegistry functionRegistry,
        INotificationRuleRunner runner,
        IRepository<NotificationSentLog, long> sentLogRepository)
    {
        _sourceRegistry = sourceRegistry;
        _functionRegistry = functionRegistry;
        _runner = runner;
        _sentLogRepository = sentLogRepository;
    }

    public Task<NotificationRuleMetadataDto> GetNotificationRuleMetadata()
    {
        var metadata = new NotificationRuleMetadataDto
        {
            Sources = _sourceRegistry.GetAll().Select(source => new NotificationRuleSourceDto
            {
                Code = source.Code,
                DisplayName = L(source.DisplayNameKey),
                HasSubType = source.SubTypeDisplayNameKey != null,
                SubTypeDisplayName = source.SubTypeDisplayNameKey == null ? null : L(source.SubTypeDisplayNameKey),
                BaseObject = ToDto(source.Criterion)
            }).ToList(),

            Functions = _functionRegistry.GetAll().Select(f => new RuleFunctionMetadataDto
            {
                Name = f.Name,
                DisplayName = L(f.DisplayNameKey),
                ReturnType = f.ReturnType,
                Parameters = f.Parameters.Select(p => new RuleFunctionParameterMetadataDto
                {
                    Name = p.Name,
                    DataType = p.DataType,
                    IsOptional = p.IsOptional
                }).ToList()
            }).ToList()
        };

        return Task.FromResult(metadata);
    }

    public async Task<List<NotificationRuleSubTypeDto>> GetSubTypes(string notificationType)
    {
        var source = _sourceRegistry.Find(notificationType);
        if (source == null || source.SubTypeDisplayNameKey == null)
        {
            return new List<NotificationRuleSubTypeDto>();
        }

        var subTypes = await source.GetSubTypesAsync();

        return subTypes.Select(s => new NotificationRuleSubTypeDto { Id = s.Id, Name = s.Name }).ToList();
    }

    public async Task<NotificationRulePreviewDto> PreviewNotificationRule(PreviewNotificationRuleInput input)
    {
        var preview = await _runner.PreviewAsync(
            input.NotificationType,
            input.NotificationSubTypeId,
            input.Criterion,
            input.NotificationMasterId,
            Clock.Now.Date);

        return new NotificationRulePreviewDto
        {
            IsValid = preview.Errors.Count == 0,
            Errors = preview.Errors.ToList(),
            MatchCount = preview.MatchCount,
            NotYetSentCount = preview.NotYetSentCount,
            Samples = preview.Samples.Select(s => new NotificationRulePreviewItemDto
            {
                RecordId = s.RecordId,
                Label = s.Label,
                SubjectDate = s.SubjectDate
            }).ToList()
        };
    }

    [AbpAuthorize(AppPermissions.Pages_SystemSecurity_NotificationMasters_Edit)]
    public async Task<int> MarkExistingAsSent(EntityDto input)
    {
        try
        {
            return await _runner.MarkExistingAsSentAsync(input.Id, Clock.Now.Date);
        }
        catch (RuleDefinitionException exception)
        {
            throw new UserFriendlyException(L("NotificationRuleCannotRun"), exception.Message);
        }
    }

    [HttpPost]
    public async Task<PagedResultDto<NotificationSentLogDto>> GetSentLogs(GetNotificationSentLogsInput input)
    {
        input.Normalize();

        var query = _sentLogRepository.GetAll()
            .WhereIf(input.NotificationMasterId.HasValue, l => l.NotificationMasterId == input.NotificationMasterId.Value)
            .WhereIf(input.FromDate.HasValue, l => l.SentDate >= input.FromDate.Value.Date)
            .WhereIf(input.ToDate.HasValue, l => l.SentDate <= input.ToDate.Value.Date);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(input.Sorting)
            .PageBy(input)
            .ToListAsync();

        return new PagedResultDto<NotificationSentLogDto>(
            totalCount,
            items.Select(l => new NotificationSentLogDto
            {
                Id = l.Id,
                NotificationMasterId = l.NotificationMasterId,
                NotificationName = l.NotificationName,
                NotificationDisplayName = L(HcmNotificationNames.DisplayNameKey(l.NotificationName)),
                SourceCode = l.SourceCode,
                RecordId = l.RecordId,
                SentDate = l.SentDate,
                RecipientCount = l.RecipientCount,
                WasSent = l.WasSent,
                IsFinal = l.IsFinal
            }).ToList());
    }

    /// <summary>
    /// The source's criterion fields in the formula editor's metadata shape. Every field is reported read-only:
    /// a criterion only tests.
    /// </summary>
    private RuleBaseObjectMetadataDto ToDto(RuleBaseObjectDescriptor descriptor)
    {
        return new RuleBaseObjectMetadataDto
        {
            Code = descriptor.Code,
            DisplayName = L(descriptor.DisplayNameKey),
            Parameters = descriptor.Parameters.Select(p => new RuleParameterMetadataDto
            {
                Name = p.Name,
                DisplayName = L(p.DisplayNameKey),
                IsWritable = false,
                Fields = p.Fields.Select(f => ToDto(f)).ToList(),
                Navigations = p.Navigations.Select(n => ToDto(n)).ToList()
            }).ToList()
        };
    }

    private RuleNavigationMetadataDto ToDto(RuleNavigationDescriptor navigation)
    {
        return new RuleNavigationMetadataDto
        {
            Name = navigation.Name,
            DisplayName = L(navigation.DisplayNameKey),
            Fields = navigation.Fields.Select(f => ToDto(f)).ToList(),
            Navigations = navigation.Navigations.Select(n => ToDto(n)).ToList()
        };
    }

    private RuleFieldMetadataDto ToDto(RuleFieldDescriptor field)
    {
        return new RuleFieldMetadataDto
        {
            Name = field.Name,
            DisplayName = L(field.DisplayNameKey),
            DataType = field.DataType,
            IsWritable = false,
            LookupSource = field.LookupSource
        };
    }
}
