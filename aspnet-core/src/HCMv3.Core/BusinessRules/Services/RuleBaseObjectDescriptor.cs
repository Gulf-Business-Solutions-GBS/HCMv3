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

    /// <summary>References a rule may follow out of this parameter, e.g. Leave to its Employee.</summary>
    public IReadOnlyList<RuleNavigationDescriptor> Navigations { get; set; } = Array.Empty<RuleNavigationDescriptor>();

    public RuleFieldDescriptor FindField(string name)
    {
        return Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    public RuleNavigationDescriptor FindNavigation(string name)
    {
        return Navigations.FirstOrDefault(n => string.Equals(n.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves a dotted path such as <c>Employee.Nationality.NameEn</c> into the references to follow and
    /// the field to read at the end, or null when any segment is not declared.
    ///
    /// <para>
    /// Returning null rather than throwing lets the caller decide: the validator turns it into an authoring
    /// error naming the path, while the evaluator turns it into a rule failure. Both are better than the
    /// thing this replaced, where an unknown path silently read as empty.
    /// </para>
    /// </summary>
    public RuleResolvedField ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var segments = path.Split('.');
        var navigations = new List<RuleNavigationDescriptor>();

        IReadOnlyList<RuleFieldDescriptor> fields = Fields;
        IReadOnlyList<RuleNavigationDescriptor> available = Navigations;

        for (var i = 0; i < segments.Length - 1; i++)
        {
            var segment = segments[i];

            var navigation = available.FirstOrDefault(n => string.Equals(n.Name, segment, StringComparison.OrdinalIgnoreCase));

            if (navigation == null)
            {
                return null;
            }

            navigations.Add(navigation);
            fields = navigation.Fields;
            available = navigation.Navigations;
        }

        var last = segments[segments.Length - 1];
        var field = fields.FirstOrDefault(f => string.Equals(f.Name, last, StringComparison.OrdinalIgnoreCase));

        return field == null ? null : new RuleResolvedField(navigations, field);
    }
}

/// <summary>
/// A reference a rule may follow: which entity sits at the other end, and the foreign key that finds it.
///
/// <para><b>Why a foreign key and not the navigation property.</b> Lazy loading is switched off in this
/// solution, so <c>absence.Employee</c> is null unless some query happened to Include it. A descriptor that
/// read the navigation property would therefore work in the one code path that eager-loads and silently
/// read as empty everywhere else -- and an empty read makes a condition quietly false, which is the failure
/// mode this engine can least afford. Declaring the foreign key instead means the engine LOADS what it
/// needs, and the cost is visible to whoever declared the reference.</para>
///
/// <para>
/// Single-valued references only. Collections would need aggregate functions ("any dependent over 18") and
/// an unbounded amount of loading; that is a separate feature, not a wider version of this one.
/// </para>
/// </summary>
public class RuleNavigationDescriptor
{
    public RuleNavigationDescriptor(
        string name,
        string displayNameKey,
        Type targetType,
        Func<object, int?> foreignKeyGetter,
        IReadOnlyList<RuleFieldDescriptor> fields,
        IReadOnlyList<RuleNavigationDescriptor> navigations)
    {
        Name = name;
        DisplayNameKey = displayNameKey;
        TargetType = targetType;
        ForeignKeyGetter = foreignKeyGetter;
        Fields = fields ?? Array.Empty<RuleFieldDescriptor>();
        Navigations = navigations ?? Array.Empty<RuleNavigationDescriptor>();
    }

    public string Name { get; }

    public string DisplayNameKey { get; }

    /// <summary>The entity to load. Must be an <c>IEntity&lt;int&gt;</c> with a registered repository.</summary>
    public Type TargetType { get; }

    /// <summary>
    /// Reads the foreign key off the owning object. Null means the reference is simply not set, which
    /// resolves the whole rest of the path to null -- an employee with no nationality is ordinary data, not
    /// an error.
    /// </summary>
    public Func<object, int?> ForeignKeyGetter { get; }

    public IReadOnlyList<RuleFieldDescriptor> Fields { get; }

    public IReadOnlyList<RuleNavigationDescriptor> Navigations { get; }
}

/// <summary>A dotted path resolved against the whitelist: references to follow, then the field to read.</summary>
public class RuleResolvedField
{
    public RuleResolvedField(IReadOnlyList<RuleNavigationDescriptor> navigations, RuleFieldDescriptor field)
    {
        Navigations = navigations ?? Array.Empty<RuleNavigationDescriptor>();
        Field = field;
    }

    public IReadOnlyList<RuleNavigationDescriptor> Navigations { get; }

    public RuleFieldDescriptor Field { get; }

    /// <summary>
    /// True when the field sits directly on the parameter. Only direct fields may be ASSIGNED: writing
    /// through a reference would mean saving a second entity as a side effect of saving the first, which is
    /// well outside what a rule should be able to do.
    /// </summary>
    public bool IsDirect => Navigations.Count == 0;
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
