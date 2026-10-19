using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Extensions;
using Abp.Runtime.Validation;
using HCMv3.BusinessRules.Dto;

namespace HCMv3.HcmNotifications.Dto;

/// <summary>What the Notification Master screen needs to build a rule: the types, and the formula editor's metadata.</summary>
public class NotificationRuleMetadataDto
{
    public List<NotificationRuleSourceDto> Sources { get; set; } = new();

    /// <summary>The business rule functions a criterion may call -- the same list the Business Rules screen offers.</summary>
    public List<RuleFunctionMetadataDto> Functions { get; set; } = new();
}

public class NotificationRuleSourceDto
{
    /// <summary>Stored in NotificationType.</summary>
    public string Code { get; set; }

    public string DisplayName { get; set; }

    /// <summary>The sub type's label (e.g. "Document Type"); null when the type has no sub type.</summary>
    public string SubTypeDisplayName { get; set; }

    public bool HasSubType { get; set; }

    /// <summary>The fields the criterion can read, in the formula editor's own metadata shape.</summary>
    public RuleBaseObjectMetadataDto BaseObject { get; set; }
}

public class NotificationRuleSubTypeDto
{
    public int Id { get; set; }

    public string Name { get; set; }
}

public class PreviewNotificationRuleInput
{
    [Required]
    [StringLength(NotificationRuleConsts.MaxSourceCodeLength)]
    public string NotificationType { get; set; }

    public int? NotificationSubTypeId { get; set; }

    public string Criterion { get; set; }

    /// <summary>The saved rule, when previewing one: lets the preview say how many it has not sent yet.</summary>
    public int? NotificationMasterId { get; set; }
}

public class NotificationRulePreviewDto
{
    /// <summary>False when the criterion cannot run; <see cref="Errors"/> says why.</summary>
    public bool IsValid { get; set; }

    public List<string> Errors { get; set; } = new();

    /// <summary>Records that match today.</summary>
    public int MatchCount { get; set; }

    /// <summary>Of those, the ones the next run would actually notify about.</summary>
    public int NotYetSentCount { get; set; }

    public List<NotificationRulePreviewItemDto> Samples { get; set; } = new();
}

public class NotificationRulePreviewItemDto
{
    public long RecordId { get; set; }

    public string Label { get; set; }

    public DateTime? SubjectDate { get; set; }
}

public class GetNotificationSentLogsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    /// <summary>One rule's history. Null lists every notice, the fixed daily ones included.</summary>
    public int? NotificationMasterId { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public void Normalize()
    {
        if (Sorting.IsNullOrWhiteSpace())
        {
            Sorting = "SentDate DESC, Id DESC";
        }
    }
}

public class NotificationSentLogDto : EntityDto<long>
{
    public int? NotificationMasterId { get; set; }

    public string NotificationName { get; set; }

    /// <summary>The definition's display name -- what the user sees on Zero's Notification settings screen.</summary>
    public string NotificationDisplayName { get; set; }

    public string SourceCode { get; set; }

    public long RecordId { get; set; }

    public DateTime SentDate { get; set; }

    public int RecipientCount { get; set; }

    public bool WasSent { get; set; }

    public bool IsFinal { get; set; }
}
