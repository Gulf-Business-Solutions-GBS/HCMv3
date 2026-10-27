using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;

namespace HCMv3.BusinessRules.Services;

/// <inheritdoc cref="IRuleFunctionRegistry"/>
/// <remarks>
/// Singleton with a lazily built index, matching <see cref="RuleBaseObjectRegistry"/>. Duplicate names
/// throw at first use rather than resolving to whichever implementation the container happened to enumerate
/// first -- a rule calling <c>Today()</c> must not mean different things in different processes.
/// </remarks>
public class RuleFunctionRegistry : IRuleFunctionRegistry, ISingletonDependency
{
    private readonly IEnumerable<IRuleFunction> _functions;
    private readonly object _syncRoot = new();

    private Dictionary<string, IRuleFunction> _byName;
    private IReadOnlyList<RuleFunctionDescriptor> _descriptors;

    public RuleFunctionRegistry(IEnumerable<IRuleFunction> functions)
    {
        _functions = functions;
    }

    public IReadOnlyList<RuleFunctionDescriptor> GetAll()
    {
        EnsureBuilt();
        return _descriptors;
    }

    public IRuleFunction Find(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        EnsureBuilt();

        return _byName.TryGetValue(name, out var function) ? function : null;
    }

    public IRuleFunction Get(string name)
    {
        return Find(name)
               ?? throw new RuleDefinitionException("There is no rule function named '" + name + "'.");
    }

    private void EnsureBuilt()
    {
        if (_byName != null)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_byName != null)
            {
                return;
            }

            var byName = new Dictionary<string, IRuleFunction>(StringComparer.OrdinalIgnoreCase);

            foreach (var function in _functions ?? Enumerable.Empty<IRuleFunction>())
            {
                if (byName.ContainsKey(function.Name))
                {
                    throw new InvalidOperationException(
                        "Two rule functions are both named '" + function.Name + "'. The second was "
                        + function.GetType().Name + ".");
                }

                byName.Add(function.Name, function);
            }

            _descriptors = byName.Values.Select(f => f.Descriptor).OrderBy(d => d.Name).ToList();
            _byName = byName;
        }
    }
}
