using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.GoLiveSetup.Dto;

/// <summary>One step's saved draft.</summary>
public class GoLiveStepStateDto
{
    public string StepId { get; set; }

    /// <summary>JSON owned by the client: <c>{ answers, notes }</c> or <c>{ rows }</c>.</summary>
    public string Payload { get; set; }

    public DateTime LastSavedUtc { get; set; }
}

public class SaveGoLiveStepInput
{
    [Required]
    [StringLength(GoLiveSetupConsts.MaxStepIdLength)]
    public string StepId { get; set; }

    /// <summary>
    /// Pass null or empty to clear the step. No length cap: an employee sheet is legitimately large, and
    /// the column is nvarchar(max).
    /// </summary>
    public string Payload { get; set; }
}

public class GetGoLiveStateOutput
{
    public List<GoLiveStepStateDto> Steps { get; set; } = new();

    /// <summary>Null until something has been saved. Shown as "last saved" in the shell.</summary>
    public DateTime? LastSavedUtc { get; set; }
}
