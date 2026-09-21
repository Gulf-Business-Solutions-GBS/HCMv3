using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Domain.Services;

namespace HCMv3.Integrations.Services;

/// <summary>
/// Records and reads back connection attempts, so a card can report when an integration last worked
/// rather than only whether its fields are filled in.
/// </summary>
public interface IIntegrationConnectionLogger : IDomainService
{
    Task LogAsync(
        string integrationKey,
        IntegrationAttemptType attemptType,
        bool succeeded,
        string message,
        int durationMs,
        int? itemCount = null);

    /// <summary>
    /// The most recent attempt of each type for one integration, keyed by attempt type. Null entries are
    /// omitted.
    /// </summary>
    Task<IReadOnlyDictionary<IntegrationAttemptType, IntegrationConnectionLog>> GetLatestAsync(string integrationKey);

    /// <summary>
    /// The most recent attempt of each type for every integration at once, keyed by integration key.
    /// One query for the whole card grid, rather than one per card.
    /// </summary>
    Task<IReadOnlyDictionary<string, IReadOnlyDictionary<IntegrationAttemptType, IntegrationConnectionLog>>> GetLatestForAllAsync();

    /// <summary>Recent history for one integration, newest first.</summary>
    Task<IReadOnlyList<IntegrationConnectionLog>> GetRecentAsync(string integrationKey, int maxCount);
}
