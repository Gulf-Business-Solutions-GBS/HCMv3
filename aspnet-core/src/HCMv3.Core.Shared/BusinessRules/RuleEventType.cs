namespace HCMv3.BusinessRules;

/// <summary>
/// The lifecycle moment a <c>BusinessRule</c> is attached to, mirroring the "rule assignment" concept in
/// SAP SuccessFactors (a rule is authored once against a base object, then assigned to one or more events).
///
/// <para>
/// The numbering is stable and persisted in <c>BusinessRuleAssignment.EventType</c> -- never renumber. New
/// events are appended.
/// </para>
/// </summary>
public enum RuleEventType
{
    /// <summary>
    /// Nothing. Present so a mis-mapped zero cannot silently mean "BeforeSave" and start mutating rows.
    /// </summary>
    None = 0,

    /// <summary>
    /// A blank record is being prepared for the user (the "GetForEdit with no id" path, and the ABP
    /// <c>EntityCreating</c> moment for defaults). Rules here fill in defaults; they must not validate,
    /// because the user has not typed anything yet.
    /// </summary>
    OnInit = 1,

    /// <summary>
    /// Immediately before the row is written, inside the saving unit of work. This is where derived fields
    /// belong (probation end date, full name, computed entitlements). Assignments run in
    /// <c>ExecutionOrder</c>, and a rule that throws aborts the save.
    /// </summary>
    BeforeSave = 2,

    /// <summary>
    /// After the row is written and its identity is known. Side effects only -- writes made to the entity
    /// here are NOT persisted by the save that just completed.
    /// </summary>
    AfterSave = 3,

    /// <summary>
    /// Validation-only pass. Rules may raise errors but writes are ignored, which lets the same rule set be
    /// replayed for a "check this record" screen without any risk of mutating it.
    /// </summary>
    OnValidate = 4,

    /// <summary>
    /// Before the row is deleted. A rule that throws blocks the delete -- the rule-authored equivalent of
    /// legacy XAF's <c>OnDeleting</c> guards.
    /// </summary>
    BeforeDelete = 5,

    /// <summary>
    /// A workflow request is choosing its approval chain. Consulted by <c>IRequestMasterResolver</c> before
    /// its ordinary workflow-group lookup, so the business team can route "if the applier is a manager, use
    /// the senior chain" without a code change. Rules here write to the routing parameter, not the entity.
    /// </summary>
    OnRoute = 6
}
