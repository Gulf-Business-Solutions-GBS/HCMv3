using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;

namespace HCMv3.Integrations.Services;

/// <inheritdoc cref="IIntegrationConnectorRegistry"/>
/// <remarks>
/// Connectors are transient domain services. They are resolved once here and released when this registry
/// is disposed -- which, being itself transient, happens at the end of the request that asked for it.
/// Resolving transients and never releasing them leaks tracked instances in the container.
/// </remarks>
public class IntegrationConnectorRegistry : IIntegrationConnectorRegistry, ITransientDependency, IDisposable
{
    private readonly IIocResolver _iocResolver;
    private readonly IReadOnlyList<IIntegrationConnector> _connectors;
    private bool _disposed;

    public IntegrationConnectorRegistry(IIocResolver iocResolver)
    {
        _iocResolver = iocResolver;

        _connectors = _iocResolver.ResolveAll<IIntegrationConnector>()
            .OrderBy(c => c.Category)
            .ThenBy(c => c.NameKey, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<IIntegrationConnector> GetAll()
    {
        return _connectors;
    }

    public IIntegrationConnector Find(string key)
    {
        return _connectors.FirstOrDefault(c => string.Equals(c.Key, key, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        foreach (var connector in _connectors)
        {
            _iocResolver.Release(connector);
        }
    }
}
