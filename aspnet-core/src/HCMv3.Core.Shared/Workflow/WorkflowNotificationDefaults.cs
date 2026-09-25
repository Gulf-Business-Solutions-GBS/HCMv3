namespace HCMv3.Workflow;

/// <summary>
/// The names of the two notification configurations every tenant is seeded with.
///
/// <para>
/// Here, in Core.Shared, because two projects that cannot reference each other both need them: the seeder in
/// EntityFrameworkCore creates the rows, and the application layer looks them up to point a newly created
/// step at one. Matching on a name in two places would be a string waiting to drift.
/// </para>
///
/// <para>
/// A name rather than a well-known id, because tenants are seeded independently and an id is only unique
/// within one of them. The name is also what an administrator sees on the configuration screen, so a
/// mismatch is visible rather than silent.
/// </para>
/// </summary>
public static class WorkflowNotificationDefaults
{
    /// <summary>Addresses whoever the request is pending with -- it has arrived and wants a decision.</summary>
    public const string ToApproverName = "Workflow - Notify Approver";

    /// <summary>Addresses the person who raised the request -- it has been decided.</summary>
    public const string ToRequesterName = "Workflow - Notify Requester";
}
