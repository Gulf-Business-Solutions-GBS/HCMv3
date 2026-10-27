namespace HCMv3.Integrations.Dto;

/// <summary>Outcome of a "Test Connection" press. <see cref="Message"/> arrives already localized.</summary>
public class IntegrationTestResultDto
{
    public bool Succeeded { get; set; }

    public string Message { get; set; }

    public int DurationMs { get; set; }
}
