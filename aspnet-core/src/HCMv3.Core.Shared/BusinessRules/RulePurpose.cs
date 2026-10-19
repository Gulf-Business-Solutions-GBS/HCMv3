namespace HCMv3.BusinessRules;

/// <summary>
/// What a rule is FOR, mirroring the SuccessFactors "Purpose" field. It is not decoration: the runner uses
/// it to decide whether a rule's writes are honoured, so a rule authored as a validation cannot quietly
/// start changing data when somebody assigns it to <see cref="RuleEventType.BeforeSave"/>.
/// </summary>
public enum RulePurpose
{
    None = 0,

    /// <summary>Computes and assigns values. Writes are applied.</summary>
    Evaluate = 1,

    /// <summary>Raises errors only. Writes are ignored even on a saving event.</summary>
    Validate = 2,

    /// <summary>Chooses a workflow chain or approver set. Writes land on the routing parameter only.</summary>
    Route = 3,

    /// <summary>
    /// Decides whether a record is due a notification (the Notification Master criterion). A test only:
    /// writes are never applied, so a criterion cannot change the record it is asking about.
    /// </summary>
    Notify = 4
}
