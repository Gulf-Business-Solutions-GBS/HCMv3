namespace HCMv3.Workflow;

/// <summary>
/// What a request is asking for, on the transaction it points at. Legacy <c>AbsenceRequest.ActionType</c>
/// (Create / Amendment / ReturnFromLeave / Cancellation), kept as a column on the request rather than worked
/// out from its payload so the approval chain can differ by action and a list can say which kind is waiting
/// without opening it.
/// </summary>
public enum WorkflowRequestAction
{
    /// <summary>The transaction itself, raised for the first time.</summary>
    New = 0,

    /// <summary>A change to a transaction that is already approved.</summary>
    Amendment = 1,

    /// <summary>A request to cancel an approved transaction.</summary>
    Cancellation = 2,

    /// <summary>The employee reporting their return (leave).</summary>
    Return = 3
}
