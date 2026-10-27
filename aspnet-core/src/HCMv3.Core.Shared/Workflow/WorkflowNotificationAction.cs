namespace HCMv3.Workflow;

/// <summary>
/// What just happened to a request, and therefore which of an approval step's notification configurations
/// applies. One per configuration column on <c>RequestDetail</c>, matching legacy.
/// </summary>
public enum WorkflowNotificationAction
{
    /// <summary>The request reached this step.</summary>
    Submit = 0,

    /// <summary>This step approved it.</summary>
    Approve = 1,

    /// <summary>This step rejected it.</summary>
    Reject = 2,

    /// <summary>This step sent it back.</summary>
    SendBack = 3,

    /// <summary>A comment was added at this step.</summary>
    Remark = 4,

    /// <summary>It was handed to somebody else at this step.</summary>
    Reassign = 5,

    /// <summary>It has been waiting at this step and somebody is being nudged.</summary>
    Reminder = 6
}
