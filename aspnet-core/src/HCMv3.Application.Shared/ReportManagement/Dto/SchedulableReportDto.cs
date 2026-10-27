using System.Collections.Generic;

namespace HCMv3.ReportManagement.Dto;

/// <summary>
/// A report the distribution module can schedule, discovered rather than registered.
/// </summary>
public class SchedulableReportDto
{
    /// <summary>Stable key stored on the schedule. For a grid report this is the app-service type name.</summary>
    public string ReportKey { get; set; }

    public ScheduledReportKind ReportKind { get; set; }

    /// <summary>Human-readable name for the wizard's dropdown.</summary>
    public string DisplayName { get; set; }

    /// <summary>Folder the report lives in, used to group the dropdown.</summary>
    public string Category { get; set; }

    /// <summary>
    /// The report's own filter parameters, read off its input DTO. Lets the wizard's Filter step offer
    /// the REAL parameters of the chosen report instead of asking the user to type names from memory.
    /// </summary>
    public List<SchedulableReportParameterDto> Parameters { get; set; } = new List<SchedulableReportParameterDto>();
}

public class SchedulableReportParameterDto
{
    public string Name { get; set; }

    /// <summary>Simplified CLR type name: string, int, bool, DateTime, or the enum's name.</summary>
    public string Type { get; set; }

    public bool IsRequired { get; set; }

    /// <summary>Populated for enum parameters so the wizard can render a dropdown.</summary>
    public List<string> AllowedValues { get; set; }
}
