using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Abp.Application.Services.Dto;
using Abp.Runtime.Validation;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.ReportManagement.Dto;

/// <summary>List row for the Report Distribution screen.</summary>
public class ReportScheduleListDto : EntityDto
{
    public string Name { get; set; }

    public ScheduledReportKind ReportKind { get; set; }

    public string ReportKey { get; set; }

    public ScheduledReportFormat Format { get; set; }

    public ReportBurstDimension BurstDimension { get; set; }

    public ReportScheduleOccurrenceType OccurrenceType { get; set; }

    public ReportRecurringPattern? RecurringPattern { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastRunTime { get; set; }

    public DateTime? NextRunTime { get; set; }

    /// <summary>Outcome of the most recent run, so the list shows failures without a drill-down.</summary>
    public ReportRunStatus? LastRunStatus { get; set; }

    public int RecipientRuleCount { get; set; }
}

public class GetAllReportSchedulesInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public string Filter { get; set; }

    public bool? IsActive { get; set; }

    public ScheduledReportKind? ReportKind { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "Name";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 10;
        }
    }
}

public class ReportScheduleFilterDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(128)]
    public string ParameterName { get; set; }

    [StringLength(1024)]
    public string ParameterValue { get; set; }

    [StringLength(64)]
    public string RelativeDateToken { get; set; }
}

public class ReportScheduleRecipientDto
{
    public int Id { get; set; }

    public ReportRecipientRule Rule { get; set; }

    public int? RoleId { get; set; }

    public bool ScopeToBurstUnit { get; set; }

    [StringLength(256)]
    public string EmailAddress { get; set; }

    /// <summary>
    /// Setting this is refused unless the caller holds
    /// <c>Pages.Reports.Distribution.BypassRecipientScope</c>. See <c>ReportScheduleRecipient</c>.
    /// </summary>
    public bool BypassRecipientScope { get; set; }
}

public class ReportScheduleDestinationDto
{
    public int Id { get; set; }

    public ReportDestinationMethod Method { get; set; }

    [StringLength(512)]
    public string EmailSubjectTemplate { get; set; }

    [StringLength(4000)]
    public string EmailBodyTemplate { get; set; }

    [StringLength(256)]
    public string HostAddress { get; set; }

    [StringLength(128)]
    public string FtpLogin { get; set; }

    /// <summary>
    /// NAME of the stored secret holding the password -- never the password. The client sends a setting
    /// name; the password itself is written through the settings API, not through this DTO, so it never
    /// travels in a report-schedule payload or lands in an audit log.
    /// </summary>
    [StringLength(128)]
    public string FtpPasswordSettingName { get; set; }

    public bool UseSftp { get; set; }

    public bool FtpPassiveMode { get; set; }

    [StringLength(512)]
    public string RemotePath { get; set; }

    [StringLength(512)]
    public string StorageFolder { get; set; }
}

public class CreateOrEditReportScheduleDto
{
    public int? Id { get; set; }

    [Required]
    [StringLength(SimpleLookupConsts.MaxNameLength)]
    public string Name { get; set; }

    public ScheduledReportKind ReportKind { get; set; }

    [Required]
    [StringLength(256)]
    public string ReportKey { get; set; }

    public ScheduledReportFormat Format { get; set; }

    [StringLength(2048)]
    public string NotificationEmails { get; set; }

    public bool NotifyOnStart { get; set; }

    public bool NotifyOnCompletion { get; set; }

    public bool IsActive { get; set; } = true;

    public ReportBurstDimension BurstDimension { get; set; }

    public int? BurstOrganizationUnitTypeId { get; set; }

    [StringLength(128)]
    public string BurstParameterName { get; set; }

    public ReportScheduleOccurrenceType OccurrenceType { get; set; }

    public ReportRecurringPattern? RecurringPattern { get; set; }

    public DateTime FirstOccurrence { get; set; }

    public DateTime? EndingOn { get; set; }

    public List<ReportScheduleFilterDto> Filters { get; set; } = new List<ReportScheduleFilterDto>();

    public List<ReportScheduleRecipientDto> Recipients { get; set; } = new List<ReportScheduleRecipientDto>();

    public List<ReportScheduleDestinationDto> Destinations { get; set; } = new List<ReportScheduleDestinationDto>();
}

public class GetReportScheduleForEditOutput
{
    public CreateOrEditReportScheduleDto ReportSchedule { get; set; }
}

// ---- run history --------------------------------------------------------------------------------

public class ReportScheduleRunDto : EntityDto<long>
{
    public int ReportScheduleId { get; set; }

    public string ReportScheduleName { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? FinishedAt { get; set; }

    public ReportRunStatus Status { get; set; }

    public bool IsManual { get; set; }

    public int SliceCount { get; set; }

    public string ErrorMessage { get; set; }
}

public class ReportScheduleRunSliceDto : EntityDto<long>
{
    public string BurstKey { get; set; }

    public string BurstKeyDisplay { get; set; }

    public string RecipientEmail { get; set; }

    public ReportDestinationMethod Method { get; set; }

    public ReportRunStatus Status { get; set; }

    public int RowCount { get; set; }

    /// <summary>
    /// Whether this copy was filtered to the recipient's own entitlement. Surfaced in the UI because
    /// "was this file scoped?" is the first question anyone asks about an unexpected delivery.
    /// </summary>
    public bool WasRecipientScoped { get; set; }

    public string FileName { get; set; }

    public string ErrorMessage { get; set; }
}

public class GetReportScheduleRunsInput : PagedAndSortedResultRequestDto, IShouldNormalize
{
    public int? ReportScheduleId { get; set; }

    public ReportRunStatus? Status { get; set; }

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Sorting))
        {
            Sorting = "StartedAt DESC";
        }

        if (MaxResultCount <= 0)
        {
            MaxResultCount = 10;
        }
    }
}

/// <summary>Result of triggering a schedule by hand from the list screen.</summary>
public class RunReportScheduleOutput
{
    public long RunId { get; set; }

    public int SliceCount { get; set; }

    public int RecipientCount { get; set; }

    public ReportRunStatus Status { get; set; }

    /// <summary>
    /// Populated when the run resolved nothing to send. Distinguishes "the org chart yielded no
    /// recipients" from "it worked and produced nothing", which look identical from the outside.
    /// </summary>
    public string Message { get; set; }
}
