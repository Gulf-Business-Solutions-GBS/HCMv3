namespace HCMv3.Integrations;

/// <summary>What an <c>IntegrationConnectionLog</c> row records.</summary>
public enum IntegrationAttemptType
{
    /// <summary>An administrator pressed "Test Connection".</summary>
    Test = 0,

    /// <summary>A real data exchange ran (a punch sync, a journal post).</summary>
    Sync = 1
}
