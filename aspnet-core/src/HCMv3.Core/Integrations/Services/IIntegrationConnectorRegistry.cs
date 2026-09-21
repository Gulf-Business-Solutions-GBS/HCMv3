using System.Collections.Generic;

namespace HCMv3.Integrations.Services;

/// <summary>
/// Finds the registered <see cref="IIntegrationConnector"/> implementations.
///
/// Registration is by discovery -- a connector is available as soon as its class exists -- so adding an
/// integration means adding one class, with no list to remember to update.
/// </summary>
public interface IIntegrationConnectorRegistry
{
    /// <summary>Every connector, ordered by category then name key, for the card grid.</summary>
    IReadOnlyList<IIntegrationConnector> GetAll();

    /// <summary>The connector for <paramref name="key"/>, or <c>null</c> if none is registered.</summary>
    IIntegrationConnector Find(string key);
}
