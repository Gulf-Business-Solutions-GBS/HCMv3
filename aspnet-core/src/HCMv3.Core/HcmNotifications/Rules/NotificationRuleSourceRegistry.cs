using System;
using System.Collections.Generic;
using System.Linq;
using Abp.Dependency;

namespace HCMv3.HcmNotifications.Rules;

/// <inheritdoc cref="INotificationRuleSourceRegistry"/>
/// <remarks>
/// Transient, like the sources it collects: they hold repositories, and a registry built per use costs a
/// handful of small objects. Duplicate codes throw rather than letting assembly scan order pick a winner.
/// </remarks>
public class NotificationRuleSourceRegistry : INotificationRuleSourceRegistry, ITransientDependency
{
    private readonly IReadOnlyList<INotificationRuleSource> _sources;

    public NotificationRuleSourceRegistry(IEnumerable<INotificationRuleSource> sources)
    {
        _sources = (sources ?? Enumerable.Empty<INotificationRuleSource>())
            .OrderBy(s => IndexOf(s.Code))
            .ToList();

        var duplicate = _sources
            .GroupBy(s => s.Code, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicate != null)
        {
            throw new InvalidOperationException(
                "More than one notification rule source declares the code '" + duplicate.Key + "': "
                + string.Join(", ", duplicate.Select(s => s.GetType().Name)) + ".");
        }
    }

    public IReadOnlyList<INotificationRuleSource> GetAll() => _sources;

    public INotificationRuleSource Find(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return _sources.FirstOrDefault(s => string.Equals(s.Code, code.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Picker order follows the declared list; anything undeclared goes last.</summary>
    private static int IndexOf(string code)
    {
        for (var i = 0; i < NotificationRuleSourceCodes.All.Count; i++)
        {
            if (string.Equals(NotificationRuleSourceCodes.All[i], code, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return int.MaxValue;
    }
}
