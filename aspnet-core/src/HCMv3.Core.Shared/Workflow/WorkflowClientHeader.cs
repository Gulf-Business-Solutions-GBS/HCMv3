namespace HCMv3.Workflow;

/// <summary>
/// The header a client sends to say what it is, so the request log can record where an action came from
/// rather than guessing.
///
/// <para>
/// Only the mobile app has to send anything. Everything else is inferred: an action with an HTTP request
/// behind it and no header is the web client, and an action with no HTTP request at all is the server
/// itself -- a background worker, an escalation, an admin tool. That keeps the burden on the one client
/// that cannot be identified any other way, since the mobile app calls exactly the same endpoints the web
/// app does.
/// </para>
/// </summary>
public static class WorkflowClientHeader
{
    /// <summary>The header name. Lower-case by convention; HTTP header matching is case-insensitive.</summary>
    public const string Name = "x-hcm-client";

    /// <summary>The one value that changes the outcome. Compared case-insensitively.</summary>
    public const string Mobile = "mobile";
}
