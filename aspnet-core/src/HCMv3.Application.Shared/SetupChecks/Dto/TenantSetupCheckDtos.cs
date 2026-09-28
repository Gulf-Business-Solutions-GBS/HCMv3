using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HCMv3.SetupChecks.Dto;

public class TenantSetupCheckOptionsDto
{
    /// <summary>The release this server runs.</summary>
    public string CurrentVersion { get; set; }

    /// <summary>Newest first.</summary>
    public List<SetupCheckReleaseDto> Releases { get; set; } = new();

    public List<SetupCheckTenantDto> Tenants { get; set; } = new();
}

public class SetupCheckReleaseDto
{
    public string Version { get; set; }

    public DateTime ReleasedOn { get; set; }

    public string Title { get; set; }

    public string Notes { get; set; }

    /// <summary>How many checks this release introduced.</summary>
    public int CheckCount { get; set; }
}

public class SetupCheckTenantDto
{
    public int Id { get; set; }

    public string TenancyName { get; set; }

    public string Name { get; set; }

    public bool IsActive { get; set; }
}

public class RunTenantSetupChecksInput
{
    [Range(1, int.MaxValue)]
    public int TenantId { get; set; }

    [Required]
    [StringLength(20)]
    public string Version { get; set; }

    /// <summary>True: only what this release added. False: everything up to and including it.</summary>
    public bool OnlyThisVersion { get; set; }
}

public class TenantSetupCheckReportDto
{
    public int TenantId { get; set; }

    public string TenantName { get; set; }

    public string Version { get; set; }

    public bool OnlyThisVersion { get; set; }

    /// <summary>The day "current" records were judged by (server's local date).</summary>
    public DateTime CheckedOn { get; set; }

    public List<TenantSetupCheckItemDto> Items { get; set; } = new();
}

public class TenantSetupCheckItemDto
{
    public string Code { get; set; }

    public string Module { get; set; }

    public string ModuleName { get; set; }

    public string IntroducedIn { get; set; }

    public SetupCheckSeverity Severity { get; set; }

    public string Title { get; set; }

    public string Why { get; set; }

    public SetupCheckOutcome Outcome { get; set; }

    public int CheckedCount { get; set; }

    public int MissingCount { get; set; }

    /// <summary>Codes only (employee, payroll area, screen) -- never names or amounts.</summary>
    public List<string> Examples { get; set; } = new();

    public string Message { get; set; }

    public string FixScreenCode { get; set; }

    public string FixScreenName { get; set; }
}
