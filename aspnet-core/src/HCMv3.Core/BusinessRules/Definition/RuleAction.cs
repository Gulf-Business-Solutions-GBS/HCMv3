using System.Collections.Generic;

namespace HCMv3.BusinessRules.Definition;

/// <summary>
/// Something a rule DOES once its condition holds -- the "Then" half of the tree.
///
/// <para>
/// Actions are still a closed set: a rule can assign a whitelisted field, raise an error, or -- on AfterSave
/// only -- create a record in another table or update a field on one. It cannot call a service or send mail.
/// </para>
///
/// <para>
/// <b>Writing other tables was a deliberate reversal.</b> This engine originally forbade it, so saving one
/// record could never silently change another. It was opened up because the business asked for follow-up
/// data changes ("when this leave is saved, record an adjustment"), and the guard moved rather than
/// disappearing. Record actions run only on <see cref="RuleEventType.AfterSave"/>, inside the same database
/// transaction as the save that triggered them, so a failure undoes both. They are held to the tables and
/// columns <c>IRuleRecordSchema</c> exposes, which excludes the login, permission and tenant tables, the
/// system columns, and every other tenant's rows. See <c>RuleEventActionPolicy</c>.
/// </para>
/// </summary>
public abstract class RuleAction
{
    public abstract string Kind { get; }
}

/// <summary>
/// Assigns a value to a writable field -- the probation-end-date derivation is exactly this action.
///
/// <para>
/// The target must be marked writable in the base object descriptor. Read-only fields exist so a rule can
/// branch on identity, status or audit columns without being able to rewrite them.
/// </para>
/// </summary>
public class RuleSetAction : RuleAction
{
    public const string KindName = "set";

    public override string Kind => KindName;

    public string Parameter { get; set; }

    public string Field { get; set; }

    public RuleExpression Value { get; set; }

    public override string ToString() => "Set " + Parameter + "." + Field;
}

/// <summary>
/// Raises a business error, aborting whatever operation the rule was running inside.
///
/// <para>
/// The message is a LOCALIZATION KEY, not literal text. A rule authored in one tenant's English must still
/// produce Arabic for an Arabic user, and letting authors type raw strings would guarantee it does not --
/// the same reason no user-facing string is hardcoded anywhere else in this codebase.
/// </para>
/// </summary>
public class RuleErrorAction : RuleAction
{
    public const string KindName = "error";

    public override string Kind => KindName;

    public string MessageKey { get; set; }

    /// <summary>Values substituted into the localized message, in order.</summary>
    public List<RuleExpression> Args { get; set; } = new();
}

/// <summary>One column a record action writes, and the value it gets.</summary>
public class RuleRecordFieldValue
{
    public string Field { get; set; }

    public RuleExpression Value { get; set; }

    public override string ToString() => Field;
}

/// <summary>
/// Creates a row in another table once the record that triggered the rule has been saved -- "when a leave of
/// this type is saved, add an adjustment for the employee".
///
/// <para>
/// <see cref="Table"/> is the entity name (<c>EmployeeAbsAdjustment</c>), not the database table name, because
/// that is the name the rest of the product uses and the one that survives a table being renamed. The tenant is
/// never taken from the rule: it is always the tenant of the save that triggered it.
/// </para>
/// </summary>
public class RuleCreateRecordAction : RuleAction
{
    public const string KindName = "createRecord";

    public override string Kind => KindName;

    public string Table { get; set; }

    public List<RuleRecordFieldValue> Values { get; set; } = new();

    public override string ToString() => "Create " + Table;
}

/// <summary>
/// Updates columns on an existing row in another table -- "when an employee is saved, flag their open
/// clearance".
///
/// <para>
/// The row is named by <see cref="RecordId"/>, an expression, so a rule can point at a row through a value it
/// already has (<c>Leave.EmployeeId</c>) rather than a hardcoded number that differs per environment. A row the
/// rule cannot see -- another tenant's, or one already deleted -- is treated as not found, and the save fails.
/// </para>
/// </summary>
public class RuleUpdateRecordAction : RuleAction
{
    public const string KindName = "updateRecord";

    public override string Kind => KindName;

    public string Table { get; set; }

    public RuleExpression RecordId { get; set; }

    public List<RuleRecordFieldValue> Values { get; set; } = new();

    public override string ToString() => "Update " + Table;
}
