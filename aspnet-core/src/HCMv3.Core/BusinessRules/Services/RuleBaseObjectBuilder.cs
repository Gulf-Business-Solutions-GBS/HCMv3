using System;
using System.Collections.Generic;
using System.Linq;

namespace HCMv3.BusinessRules.Services;

/// <summary>
/// Fluent, compile-checked way to declare a <see cref="RuleBaseObjectDescriptor"/>.
///
/// <para>
/// The builder exists so that exposing a field to rule authors is a one-line, type-safe statement. Its
/// value is what it makes impossible: you cannot expose a field without also saying how to read it, you
/// cannot make a field writable without providing a setter, and a property rename breaks the build here
/// rather than producing a rule that quietly stops matching in production.
/// </para>
/// </summary>
public class RuleBaseObjectBuilder<TEntity> where TEntity : class
{
    private readonly string _code;
    private readonly string _displayNameKey;
    private readonly List<RuleEventType> _events = new();
    private readonly List<RuleParameterDescriptor> _parameters = new();

    private RuleBaseObjectBuilder(string code, string displayNameKey)
    {
        _code = code;
        _displayNameKey = displayNameKey;
    }

    /// <summary>Starts a descriptor for an entity type. <paramref name="code"/> is what rules persist.</summary>
    public static RuleBaseObjectBuilder<TEntity> For(string code, string displayNameKey)
    {
        return new RuleBaseObjectBuilder<TEntity>(code, displayNameKey);
    }

    /// <summary>The lifecycle events an assignment may bind this base object to.</summary>
    public RuleBaseObjectBuilder<TEntity> SupportsEvents(params RuleEventType[] events)
    {
        _events.AddRange(events);
        return this;
    }

    /// <summary>
    /// Declares a parameter and its fields. Call once per object the rule can see -- typically the record
    /// itself, plus any read-only companions such as the original record or a routing context.
    /// </summary>
    public RuleBaseObjectBuilder<TEntity> Parameter<TParameter>(
        string name,
        string displayNameKey,
        bool isWritable,
        Action<RuleParameterBuilder<TParameter>> configure)
        where TParameter : class
    {
        var builder = new RuleParameterBuilder<TParameter>();
        configure(builder);

        _parameters.Add(new RuleParameterDescriptor(
            name,
            displayNameKey,
            typeof(TParameter),
            isWritable,
            builder.Build())
        {
            Navigations = builder.BuildNavigations()
        });

        return this;
    }

    public RuleBaseObjectDescriptor Build()
    {
        if (_parameters.Count == 0)
        {
            throw new InvalidOperationException(
                "Base object '" + _code + "' declares no parameters, so no rule written against it could read anything.");
        }

        var duplicate = _parameters
            .GroupBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            // Two parameters of the same name would make "Employee.JoiningDate" ambiguous, and the winner
            // would depend on declaration order. Refuse rather than pick.
            throw new InvalidOperationException(
                "Base object '" + _code + "' declares the parameter '" + duplicate.Key + "' more than once.");
        }

        return new RuleBaseObjectDescriptor(_code, _displayNameKey, typeof(TEntity), _events, _parameters);
    }
}

/// <summary>Declares the fields of one parameter. See <see cref="RuleBaseObjectBuilder{TEntity}"/>.</summary>
public class RuleParameterBuilder<TParameter> where TParameter : class
{
    private readonly List<RuleFieldDescriptor> _fields = new();
    private readonly List<RuleNavigationDescriptor> _navigations = new();

    /// <summary>Exposes a field a rule may read but not change.</summary>
    public RuleParameterBuilder<TParameter> Read<TValue>(
        string name,
        string displayNameKey,
        RuleDataType dataType,
        Func<TParameter, TValue> getter,
        string lookupSource = null)
    {
        _fields.Add(new RuleFieldDescriptor(
            name,
            displayNameKey,
            dataType,
            typeof(TValue),
            target => getter((TParameter)target),
            setter: null,
            lookupSource));

        return this;
    }

    /// <summary>
    /// Exposes a field a rule may read and assign. Writes are converted to <typeparamref name="TValue"/>
    /// through <see cref="RuleValueConverter"/>, so a rule assigning an integer to a decimal field, or a
    /// parsed date to a <c>DateTime</c>, does the sensible thing instead of throwing a cast error.
    /// </summary>
    public RuleParameterBuilder<TParameter> ReadWrite<TValue>(
        string name,
        string displayNameKey,
        RuleDataType dataType,
        Func<TParameter, TValue> getter,
        Action<TParameter, TValue> setter,
        string lookupSource = null)
    {
        _fields.Add(new RuleFieldDescriptor(
            name,
            displayNameKey,
            dataType,
            typeof(TValue),
            target => getter((TParameter)target),
            (target, value) => setter((TParameter)target, (TValue)RuleValueConverter.ConvertTo(value, typeof(TValue))),
            lookupSource));

        return this;
    }

    /// <summary>
    /// Declares a reference a rule may follow, so an author can write
    /// <c>Leave.Employee.Nationality.NameEn</c> instead of memorising a nationality id.
    ///
    /// <para>
    /// <paramref name="foreignKey"/> is how the target is FOUND -- the engine loads it rather than reading a
    /// navigation property, because lazy loading is off in this solution and an unloaded navigation would
    /// read as empty and quietly make the condition false. See <see cref="RuleNavigationDescriptor"/>.
    /// </para>
    ///
    /// <para>
    /// Each reference costs one keyed lookup the first time a rule touches it, memoised for the rest of that
    /// execution. That is cheap, but it is not free: declare references that rules genuinely need, not the
    /// whole schema.
    /// </para>
    /// </summary>
    public RuleParameterBuilder<TParameter> Navigate<TTarget>(
        string name,
        string displayNameKey,
        Func<TParameter, int?> foreignKey,
        Action<RuleParameterBuilder<TTarget>> configure)
        where TTarget : class
    {
        var builder = new RuleParameterBuilder<TTarget>();
        configure(builder);

        _navigations.Add(new RuleNavigationDescriptor(
            name,
            displayNameKey,
            typeof(TTarget),
            owner => foreignKey((TParameter)owner),
            builder.Build(),
            builder.BuildNavigations()));

        return this;
    }

    internal IReadOnlyList<RuleFieldDescriptor> Build()
    {
        var duplicate = _fields
            .GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new InvalidOperationException(
                "The field '" + duplicate.Key + "' is declared more than once on parameter type "
                + typeof(TParameter).Name + ".");
        }

        return _fields;
    }

    internal IReadOnlyList<RuleNavigationDescriptor> BuildNavigations()
    {
        var clash = _navigations
            .Select(n => n.Name)
            .Intersect(_fields.Select(f => f.Name), StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault();

        if (clash != null)
        {
            // "Employee" as both a field and a reference would make Leave.Employee ambiguous, and which one
            // won would depend on resolution order. Refuse rather than pick.
            throw new InvalidOperationException(
                "'" + clash + "' is declared as both a field and a reference on parameter type "
                + typeof(TParameter).Name + ".");
        }

        var duplicate = _navigations
            .GroupBy(n => n.Name, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new InvalidOperationException(
                "The reference '" + duplicate.Key + "' is declared more than once on parameter type "
                + typeof(TParameter).Name + ".");
        }

        return _navigations;
    }
}
