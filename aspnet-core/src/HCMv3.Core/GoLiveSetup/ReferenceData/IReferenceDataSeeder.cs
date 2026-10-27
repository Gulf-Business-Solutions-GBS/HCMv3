using System.Collections.Generic;
using System.Threading.Tasks;

namespace HCMv3.GoLiveSetup.ReferenceData;

/// <summary>
/// Fills a tenant's reference tables (countries, nationalities, titles, currencies, Kuwait geography,
/// banks…) from the packs shipped with the product.
///
/// <para><b>Why this exists.</b> Before it, a newly created tenant had completely empty reference
/// tables: nothing in the solution seeded <c>Nationality</c>, <c>Country</c>, <c>Currency</c> or any
/// other lookup -- there were no HR seeders and no <c>HasData</c> calls anywhere. Every onboarding
/// therefore started with somebody typing lists by hand, and neither Go-Live Setup nor the offline
/// workbook can function without them: the customer must never be asked to supply 254 nationalities.</para>
///
/// <para><b>Idempotent.</b> A row is matched on <c>NameEn</c>, case-insensitively, and skipped if the
/// tenant already has it. Re-running is therefore safe, which matters because the main use today is
/// backfilling tenants that were created before this existed.</para>
/// </summary>
public interface IReferenceDataSeeder
{
    /// <summary>Pack ids available to seed, in the order they should be applied.</summary>
    IReadOnlyList<string> GetAvailablePackIds();

    /// <summary>Loads a pack without writing anything -- used by tests and by the preview on the UI.</summary>
    ReferenceDataPack GetPack(string packId);

    /// <summary>
    /// Counts what a pack would add to the current tenant without writing anything. Backs the "what
    /// will this do" panel, and keeps the pack-to-repository mapping in one place instead of letting
    /// the application layer rebuild it.
    /// </summary>
    Task<ReferenceDataSeedResult> PreviewPackAsync(string packId);

    /// <summary>
    /// Seeds one pack into one tenant. The caller owns the unit of work and the tenant scope;
    /// <paramref name="tenantId"/> is stamped on every inserted row explicitly, matching the convention
    /// every application service here follows.
    /// </summary>
    Task<ReferenceDataSeedResult> SeedPackAsync(string packId, int tenantId);

    /// <summary>
    /// Seeds the packs a new tenant should always get. Currently <c>universal</c> plus <c>kuwait</c>,
    /// because every client so far operates there; when that stops being true this becomes a choice on
    /// the tenant-creation screen rather than a constant.
    /// </summary>
    Task<List<ReferenceDataSeedResult>> SeedDefaultsAsync(int tenantId);
}
