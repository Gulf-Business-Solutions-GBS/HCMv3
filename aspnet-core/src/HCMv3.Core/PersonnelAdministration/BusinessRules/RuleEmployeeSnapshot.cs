using System;
using HCMv3.GeneralMasterInfo;

namespace HCMv3.PersonnelAdministration.BusinessRules;

/// <summary>
/// A flat, already-resolved picture of an employee for rules to branch on: their current org assignment,
/// their job title, and whether anybody reports to them.
///
/// <para>
/// A snapshot rather than the <see cref="Employee"/> entity itself, and that is the important design point.
/// The facts a routing rule needs -- "is this person a manager", "what is their job title today" -- are not
/// columns on Employee; they live in dated <c>EmployeeOrgAssignment</c> and <c>EmployeeManager</c> rows.
/// Exposing the entity and letting rules navigate to them would mean lazy loads inside a save, one query
/// per rule, with the cost invisible to the person authoring the rule. Building the snapshot once in the
/// caller makes that cost explicit, bounded, and paid exactly once per request.
/// </para>
///
/// <para>
/// It is also what keeps the rule vocabulary stable. <c>Applier.IsManager</c> stays true regardless of how
/// many times the underlying manager model is refactored, because the mapping lives in one place instead of
/// in every tenant's stored rules.
/// </para>
/// </summary>
public class RuleEmployeeSnapshot
{
    public int EmployeeId { get; set; }

    public string Code { get; set; }

    public DateTime JoiningDate { get; set; }

    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// Needed by eligibility rules that depend on it -- maternity leave being the obvious one. Read from
    /// the employee record, not inferred from anything else.
    /// </summary>
    public Gender Gender { get; set; }

    /// <summary>From the org assignment effective on the execution date.</summary>
    public int? JobTitleId { get; set; }

    /// <summary>
    /// The job title's business code, e.g. "CHRO".
    ///
    /// <para>
    /// Rules should prefer this over <see cref="JobTitleId"/>. Ids differ per tenant and per environment,
    /// so a rule written against an id is a rule that silently matches nothing after a data refresh, while
    /// a code is the same string a configurer reads on the job title screen.
    /// </para>
    /// </summary>
    public string JobTitleCode { get; set; }

    public int? EmployeeGroupId { get; set; }

    public int? EmployeeSubGroupId { get; set; }

    public long? OrganizationUnitId { get; set; }

    public int? CostCenterId { get; set; }

    public int? WorkLocationId { get; set; }

    public int? WorkflowGroupId { get; set; }

    /// <summary>True when at least one employee currently reports to this one.</summary>
    public bool IsManager { get; set; }

    /// <summary>
    /// How many people report directly to them. Lets a rule distinguish a team lead from a department head
    /// without needing a separate seniority field that nobody maintains.
    /// </summary>
    public int DirectReportCount { get; set; }
}
