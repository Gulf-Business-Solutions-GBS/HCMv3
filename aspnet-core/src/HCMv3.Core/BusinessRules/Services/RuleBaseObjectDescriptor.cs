using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Declares an entity as rule-authorable: which parameters a rule sees, which fields of those parameters it
/// may read or write, and which lifecycle events it may be armed on.
///
/// <para>
/// This is the security boundary of the whole engine, and it is a WHITELIST rather than a filter. The
/// evaluator cannot reach a field that has no descriptor -- not because it checks a deny-list, but because
/// the only way it can read anything at all is through the accessor delegates declared here. A rule author
/// therefore cannot navigate to another tenant's data, walk a lazy-loaded graph inside a save, or write an
/// audit column, regardless of what JSON they post.
/// </para>
///
/// <para>
/// The second job it does is feed the authoring UI. The field list drives the builder's picker, so the two
/// can never disagree about what is available -- a field the UI offers is by construction a field the
/// evaluator can read.
/// </para>
/// </summary>
public class RuleBaseObjectDescriptor
{
    public RuleBaseObjectDescriptor(
        string code,
        string displayNameKey,
        Type clrType,
        IReadOnlyList<RuleEventType> supportedEvents,
        IReadOnlyList<RuleParameterDescriptor> parameters)
    {
        Code = code;
        DisplayNameKey = displayNameKey;
        ClrType = clrType;
        SupportedEvents = supportedEvents ?? Array.Empty<RuleEventType>();
        Parameters = parameters ?? Array.Empty<RuleParameterDescriptor>();
    }

    /// <summary>Stable identifier stored in <c>BusinessRule.BaseObject</c>. Never renamed.</summary>
    public string Code { get; }

    /// <summary>Localization key for the display name. Not the display name itself -- see the localization rules.</summary>
    public string DisplayNameKey { get; }

    /// <summary>The primary entity type, used to bind entity events to this descriptor.</summary>
    public Type ClrType { get; }

    /// <summary>
    /// The events an assignment may bind this base object to. Narrow deliberately: offering
    /// <c>OnRoute</c> on an entity that never enters workflow would let somebody arm a rule that can never
    /// fire and then spend a day wondering why.
    /// </summary>
    public IReadOnlyList<RuleEventType> SupportedEvents { get; }

    public IReadOnlyList<RuleParameterDescriptor> Parameters { get; }

    public RuleParameterDescriptor FindParameter(string name)
    {
        return Parameters.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public bool Supports(RuleEventType eventType) => SupportedEvents.Contains(eventType);
}

/// <summary>
/// One named object a rule can see, mirroring the Parameters table in the SuccessFactors rule header
/// (Employee Time / Original Record / Rule Context, each with its own access level).
///
/// <para>
/// Several parameters rather than one is what makes non-trivial rules expressible: comparing the record
/// being saved against the record as it was ("Original Record") is impossible with a single parameter, and
/// it is exactly what a rule like "block a backdated joining date change" needs.
/// </para>
/// </summary>
public class RuleParameterDescriptor
{
    public RuleParameterDescriptor(
        string name,
        string displayNameKey,
        Type clrType,
        bool isWritable,
        IReadOnlyList<RuleFieldDescriptor> fields)
    {
        Name = name;
        DisplayNameKey = displayNameKey;
        ClrType = clrType;
        IsWritable = isWritable;
        Fields = fields ?? Array.Empty<RuleFieldDescriptor>();
    }

    public string Name { get; }

    public string DisplayNameKey { get; }

    public Type ClrType { get; }

    /// <summary>
    /// Whether the parameter as a whole accepts writes. A field may be individually writable and still be
    /// unwritable through a read-only parameter -- both gates must open, so exposing the same entity twice
    /// (once as the live record, once as the original) cannot accidentally make history editable.
    /// </summary>
    public bool IsWritable { get; }

    public IReadOnlyList<RuleFieldDescriptor> Fields { get; }

    public RuleFieldDescriptor FindField(string name)
    {
        return Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// One readable (and possibly writable) field.
///
/// <para>
/// Access is by DELEGATE, not by reflection over a property name. Reflection would work, but it would mean
/// the whitelist is a list of strings checked at runtime, and a typo in that list becomes a field that
/// silently does not exist. A compiled accessor is checked by the compiler, costs no reflection on the hot
/// path, and makes renaming the underlying property a build error rather than a support ticket.
/// </para>
/// </summary>
public class RuleFieldDescriptor
{
    public RuleFieldDescriptor(
        string name,
        string displayNameKey,
        RuleDataType dataType,
        Type clrType,
        Func<object, object> getter,
        Action<object, object> setter,
        string lookupSource)
    {
        Name = name;
        DisplayNameKey = displayNameKey;
        DataType = dataType;
        ClrType = clrType;
        Getter = getter;
        Setter = setter;
        LookupSource = lookupSource;
    }

    public string Name { get; }

    public string DisplayNameKey { get; }

    public RuleDataType DataType { get; }

    /// <summary>The underlying CLR type, so a write can be converted precisely rather than approximately.</summary>
    public Type ClrType { get; }

    public Func<object, object> Getter { get; }

    /// <summary>Null when the field is read-only.</summary>
    public Action<object, object> Setter { get; }

    public bool IsWritable => Setter != null;

    /// <summary>
    /// Names the lookup or enum behind a <see cref="RuleDataType.Lookup"/> or <see cref="RuleDataType.Enum"/>
    /// field, so the authoring UI can offer a picker of real values instead of asking a configurer to type a
    /// numeric id. Null for plain fields.
    /// </summary>
    public string LookupSource { get; }
}
