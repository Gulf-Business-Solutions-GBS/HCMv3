using System;

namespace HCMv3.Integrations.Dto;

/// <summary>One recorded attempt to reach the external system.</summary>
public class IntegrationAttemptDto
{
    public DateTime Time { get; set; }

    public IntegrationAttemptType AttemptType { get; set; }

    public bool Succeeded { get; set; }

    public string Message { get; set; }

    public int? ItemCount { get; set; }

    public int DurationMs { get; set; }
}
