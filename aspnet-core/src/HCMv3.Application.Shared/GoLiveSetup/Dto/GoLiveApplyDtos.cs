using System.Collections.Generic;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>What one Apply run did, or would do.</summary>
public class GoLiveApplyResultDto
{
    /// <summary>False for a preview. A preview walks the identical code path and writes nothing.</summary>
    public bool Applied { get; set; }

    public List<GoLiveApplyStageDto> Stages { get; set; } = new();

    public int TotalCreated { get; set; }

    public int TotalUpdated { get; set; }

    public int TotalSkipped { get; set; }

    public int TotalFailed { get; set; }

    /// <summary>True when nothing failed. The gate for turning a preview into a real run.</summary>
    public bool IsClean => TotalFailed == 0;
}

public class GoLiveApplyStageDto
{
    /// <summary>Registry step the stage reads, e.g. <c>S3.1</c>.</summary>
    public string StepId { get; set; }

    /// <summary>What it provisions, e.g. <c>Employee</c>.</summary>
    public string Target { get; set; }

    public int Created { get; set; }

    public int Updated { get; set; }

    /// <summary>Rows already matching what is stored, left alone.</summary>
    public int Skipped { get; set; }

    public int Failed { get; set; }

    /// <summary>Why rows failed. Capped so one broken sheet cannot return megabytes.</summary>
    public List<GoLiveApplyIssueDto> Issues { get; set; } = new();

    /// <summary>Failures beyond the ones listed.</summary>
    public int IssuesTruncated { get; set; }
}

public class GoLiveApplyIssueDto
{
    /// <summary>1-based row number as the customer sees it in the grid.</summary>
    public int Row { get; set; }

    /// <summary>The row's natural key, so the message is useful without the sheet open.</summary>
    public string Key { get; set; }

    public string Message { get; set; }
}
