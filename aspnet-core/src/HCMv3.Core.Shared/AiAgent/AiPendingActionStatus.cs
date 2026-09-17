namespace HCMv3.AiAgent;

/// <summary>
/// Where an action the assistant prepared stands. The assistant can only ever create one in
/// <see cref="Pending"/>; every other state is reached by the user pressing a button.
/// </summary>
public enum AiPendingActionStatus
{
    /// <summary>Prepared by the assistant, waiting for the user to confirm or cancel.</summary>
    Pending = 0,

    /// <summary>The user confirmed and the underlying request succeeded.</summary>
    Completed = 1,

    /// <summary>The user cancelled it.</summary>
    Cancelled = 2,

    /// <summary>The user confirmed but the underlying request was refused, e.g. not enough balance.</summary>
    Failed = 3,

    /// <summary>Left unconfirmed too long, or superseded by a newer draft in the same conversation.</summary>
    Expired = 4,

    /// <summary>
    /// Confirmed and running. Claimed with a single conditional update from <see cref="Pending"/>, so a
    /// double-click or two open tabs cannot submit the same leave twice: only one claim changes a row.
    /// </summary>
    Processing = 5
}
