using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;

namespace HCMv3.BusinessRules.Services;

/// <inheritdoc cref="IRuleBaseObjectRegistry"/>
/// <remarks>
/// <para>
/// Built once and cached for the life of the process: descriptors are static metadata declared in code, so
/// rebuilding them per request would burn allocations for an answer that cannot have changed. The build is
/// lazy and locked rather than done in the constructor, because a provider is free to touch other
/// singletons and constructor-time graph walking is how circular-dependency failures at startup happen.
/// </para>
/// <para>
/// Duplicate codes THROW. Two descriptors claiming "Employee" would mean the field a rule resolves depends
/// on assembly scan order -- so the rule works on one developer's machine and not on the build server. A
/// startup failure naming both providers is the kinder outcome.
/// </para>
/// </remarks>
public class RuleBaseObjectRegistry : IRuleBaseObjectRegistry, ISingletonDependency
{
    private readonly IEnumerable<IRuleBaseObjectProvider> _providers;
    private readonly object _syncRoot = new();

    private IReadOnlyList<RuleBaseObjectDescriptor> _descriptors;
    private Dictionary<string, RuleBaseObjectDescriptor> _byCode;

    public RuleBaseObjectRegistry(IEnumerable<IRuleBaseObjectProvider> providers)
    {
        _providers = providers;
    }

    public IReadOnlyList<RuleBaseObjectDescriptor> GetAll()
    {
        EnsureBuilt();
        return _descriptors;
    }

    public RuleBaseObjectDescriptor Find(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        EnsureBuilt();

        return _byCode.TryGetValue(code, out var descriptor) ? descriptor : null;
    }

    public RuleBaseObjectDescriptor Get(string code)
    {
        return Find(code)
               ?? throw new RuleDefinitionException("There is no rule base object named '" + code + "'.");
    }

    public RuleBaseObjectDescriptor FindByClrType(Type clrType)
    {
        if (clrType == null)
        {
            return null;
        }

        EnsureBuilt();

        return _descriptors.FirstOrDefault(d => d.ClrType == clrType);
    }

    private void EnsureBuilt()
    {
        if (_descriptors != null)
        {
            return;
        }

        lock (_syncRoot)
        {
            if (_descriptors != null)
            {
                return;
            }

            var descriptors = new List<RuleBaseObjectDescriptor>();
            var byCode = new Dictionary<string, RuleBaseObjectDescriptor>(StringComparer.OrdinalIgnoreCase);

            foreach (var provider in _providers ?? Enumerable.Empty<IRuleBaseObjectProvider>())
            {
                foreach (var descriptor in provider.GetBaseObjects())
                {
                    if (byCode.ContainsKey(descriptor.Code))
                    {
                        throw new InvalidOperationException(
                            "Two rule base object providers both declare the code '" + descriptor.Code
                            + "'. The second was " + provider.GetType().Name + ".");
                    }

                    byCode.Add(descriptor.Code, descriptor);
                    descriptors.Add(descriptor);
                }
            }

            _byCode = byCode;
            _descriptors = descriptors;
        }
    }
}
