using System;
using System.Collections.Generic;
using System.Text;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Everything one rule execution can see and everything it did: the parameter objects, the variables it
/// computed, the date it considers "today", and the trace of what happened.
///
/// <para>
/// Deliberately mutable and deliberately short-lived. One instance is built per event, shared by every rule
/// armed on that event, and discarded. Sharing it is what lets a derivation rule and the validation rule
/// after it agree about the record -- the second sees the first's writes, because they are writes to the
/// same object, not to a copy.
/// </para>
///
/// <para>
/// Nothing here is thread-safe and nothing needs to be: a context belongs to the save it was created for.
/// </para>
/// </summary>
public class RuleExecutionContext
{
    private readonly Dictionary<string, object> _parameters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, object> _variables = new(StringComparer.OrdinalIgnoreCase);
    private readonly StringBuilder _trace = new();

    public RuleExecutionContext(
        RuleBaseObjectDescriptor baseObject,
        RuleEventType eventType,
        DateTime executionDate,
        int? tenantId)
    {
        BaseObject = baseObject ?? throw new ArgumentNullException(nameof(baseObject));
        EventType = eventType;
        ExecutionDate = executionDate.Date;
        TenantId = tenantId;
    }

    public RuleBaseObjectDescriptor BaseObject { get; }

    public RuleEventType EventType { get; }

    /// <summary>
    /// What <c>Today()</c> returns and what effective dating is measured against.
    ///
    /// <para>
    /// Passed in rather than read from the clock, for two reasons that both matter. Tests need a fixed
    /// date to assert against, and a long-running batch must not have rules change their minds halfway
    /// through because it crossed midnight.
    /// </para>
    /// </summary>
    public DateTime ExecutionDate { get; }

    public int? TenantId { get; }

    /// <summary>
    /// Whether assignments are honoured. False for validation-purpose rules and for read-only events, so a
    /// rule's writes are dropped rather than applied when it is running somewhere writes are not wanted.
    /// The evaluator still evaluates the value expression -- a write that would have thrown still throws,
    /// which is what makes a dry run worth anything.
    /// </summary>
    public bool AllowWrites { get; set; } = true;

    /// <summary>Set by the runner while a rule is executing, so errors and trace lines can name it.</summary>
    public string CurrentRuleCode { get; set; }

    /// <summary>
    /// Send execution logging to the application log instead of the execution log TABLE.
    ///
    /// <para>
    /// Set by callers that run rules from inside an active <c>SaveChanges</c>. Writing a log row there would
    /// mean opening a second transaction while the first is mid-flight -- on SQL Server that risks promoting
    /// the whole thing to a distributed transaction, which is a steep price for an audit row. The trade-off
    /// is deliberate and worth knowing: for BeforeSave rules the failure detail lands in the application log
    /// and in the exception the user sees, not in the execution log table. Routing rules and the test
    /// harness, which run outside a save, log normally.
    /// </para>
    /// </summary>
    public bool SuppressDatabaseLogging { get; set; }

    /// <summary>
    /// Remaining evaluation steps, decremented by the evaluator. Guards against a rule that is expensive by
    /// accident -- a deeply nested tree over a list, say -- holding a save open. Shared across the whole
    /// event so one pathological rule cannot be hidden by being split into several.
    /// </summary>
    public int RemainingSteps { get; set; } = BusinessRuleConsts.MaxExecutionSteps;

    /// <summary>Fields actually assigned during this execution, for the log line and the test harness.</summary>
    public List<RuleFieldWrite> Writes { get; } = new();

    /// <summary>
    /// Loads the entity at the far end of a declared reference. Null disables reference following, which is
    /// what the dry-run harness does -- there is no real row to walk out of.
    /// </summary>
    public IRuleEntityLoader EntityLoader { get; set; }

    private readonly Dictionary<string, object> _loadedEntities = new();

    /// <summary>
    /// Follows one declared reference, memoised for the rest of this execution.
    ///
    /// <para>
    /// The memo is what keeps reference-following affordable: a rule reading three fields off
    /// <c>Leave.Employee</c> loads that employee once, and two rules armed on the same event share the load
    /// because they share the context. Misses are cached too -- an id that resolves to nothing must not be
    /// re-queried on every read.
    /// </para>
    /// </summary>
    public object FollowReference(RuleNavigationDescriptor navigation, object owner)
    {
        if (owner == null || navigation == null)
        {
            return null;
        }

        var foreignKey = navigation.ForeignKeyGetter(owner);

        // An unset reference is ordinary data -- an employee with no nationality -- so it reads as null
        // rather than failing.
        if (!foreignKey.HasValue)
        {
            return null;
        }

        if (EntityLoader == null)
        {
            throw new RuleExecutionException(
                CurrentRuleCode,
                "This rule follows the reference '" + navigation.Name
                + "', which is not available in this context.");
        }

        var key = navigation.TargetType.FullName + "#" + foreignKey.Value;

        if (_loadedEntities.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var loaded = EntityLoader.Load(navigation.TargetType, foreignKey.Value);

        _loadedEntities[key] = loaded;

        return loaded;
    }

    /// <summary>
    /// Walks a resolved path to the object that owns the final field, or null when any reference along the
    /// way is unset.
    /// </summary>
    public object ResolveOwner(RuleResolvedField resolved, object root)
    {
        var current = root;

        foreach (var navigation in resolved.Navigations)
        {
            if (current == null)
            {
                return null;
            }

            current = FollowReference(navigation, current);
        }

        return current;
    }

    /// <summary>Registers a parameter object. Null objects are allowed -- a rule may branch on their absence.</summary>
    public RuleExecutionContext WithParameter(string name, object value)
    {
        _parameters[name] = value;
        return this;
    }

    /// <summary>
    /// The object behind a parameter name. Throws when the name is not declared, because a rule referring
    /// to a parameter the caller never supplied is a wiring bug, and treating it as null would turn that
    /// bug into a condition that quietly evaluates to false.
    /// </summary>
    public object GetParameter(string name)
    {
        if (!_parameters.TryGetValue(name, out var value))
        {
            throw new RuleExecutionException(
                CurrentRuleCode,
                "The parameter '" + name + "' was not supplied for base object '" + BaseObject.Code + "'.");
        }

        return value;
    }

    public bool HasParameter(string name) => _parameters.ContainsKey(name);

    public void SetVariable(string name, object value) => _variables[name] = value;

    public bool TryGetVariable(string name, out object value) => _variables.TryGetValue(name, out value);

    /// <summary>
    /// Clears variables between rules. Variables are scoped to ONE rule -- letting them leak into the next
    /// would make a rule's behaviour depend on which rules ran before it, which is precisely the kind of
    /// spooky action that makes configurable logic impossible to reason about.
    /// </summary>
    public void ResetVariables() => _variables.Clear();

    public void Trace(string message)
    {
        if (_trace.Length < BusinessRuleConsts.MaxDescriptionLength)
        {
            _trace.Append(message).Append("; ");
        }
    }

    public string GetTrace()
    {
        var text = _trace.ToString().TrimEnd(' ', ';');

        return text.Length > BusinessRuleConsts.MaxDescriptionLength
            ? text.Substring(0, BusinessRuleConsts.MaxDescriptionLength)
            : text;
    }

    public void ClearTrace() => _trace.Clear();
}

/// <summary>A single field assignment made by a rule. Recorded so a trace can say what changed, not just that something did.</summary>
public class RuleFieldWrite
{
    public RuleFieldWrite(string ruleCode, string parameter, string field, object value)
    {
        RuleCode = ruleCode;
        Parameter = parameter;
        Field = field;
        Value = value;
    }

    public string RuleCode { get; }

    public string Parameter { get; }

    public string Field { get; }

    public object Value { get; }

    public override string ToString()
    {
        return Parameter + "." + Field + " = " + RuleValueConverter.FormatInvariant(Value);
    }
}
