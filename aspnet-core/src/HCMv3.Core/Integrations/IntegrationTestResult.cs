namespace HCMv3.Integrations;

/// <summary>
/// Outcome of a "Test Connection" press. A test must genuinely reach the external system -- acquire the
/// OAuth token, send the mail -- because the green tick on the card is read as proof that it works.
/// </summary>
public class IntegrationTestResult
{
    public bool Succeeded { get; set; }

    /// <summary>Already-localized outcome message, shown verbatim to the administrator.</summary>
    public string Message { get; set; }

    public int DurationMs { get; set; }

    public static IntegrationTestResult Success(string message) =>
        new() { Succeeded = true, Message = message };

    public static IntegrationTestResult Failure(string message) =>
        new() { Succeeded = false, Message = message };
}
