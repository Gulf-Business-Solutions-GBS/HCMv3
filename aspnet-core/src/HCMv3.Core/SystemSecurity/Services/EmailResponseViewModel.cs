namespace HCMv3.SystemSecurity.Services;

/// <summary>
/// Result of a single templated-email send attempt (ported from legacy XAF
/// <c>HCM10.Module.BusinessObjects.Services.EmailService.EmailResponseViewModel</c>).
/// </summary>
public class EmailResponseViewModel
{
    public bool Status { get; set; }

    public string Response { get; set; }

    public string EmailSubject { get; set; }

    public string EmailBody { get; set; }

    public string EmailTo { get; set; }
}
