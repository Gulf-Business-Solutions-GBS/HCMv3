using System.Collections.Generic;
using System.Threading.Tasks;
using Abp.Application.Services;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <summary>
/// Reference data for Go-Live Setup: what the product ships, what this tenant already has, and the
/// action that closes the gap.
///
/// <para>New tenants get this automatically during creation. This surface exists for the tenants
/// created before that hook existed, whose lookup tables are empty, and for re-running a pack after a
/// list is extended in a later release.</para>
/// </summary>
public interface IReferenceDataAppService : IApplicationService
{
    /// <summary>
    /// The available packs with a per-lookup count of what would be added versus what is already there.
    /// Reads only -- nothing is written.
    /// </summary>
    Task<List<ReferenceDataPackDto>> GetPacks();

    /// <summary>Applies a pack (or every default pack) to the current tenant. Idempotent.</summary>
    Task<SeedReferenceDataOutput> SeedReferenceData(SeedReferenceDataInput input);

    /// <summary>
    /// Every dropdown list Go-Live Setup needs, in one call.
    ///
    /// <para>One request rather than a dozen because the step screens need several lists at once and
    /// the whole payload is a few kilobytes of names. Mixes seeded reference data (nationalities,
    /// banks) with data the customer creates themselves (job titles, work locations, cost centres) --
    /// the client does not care which is which, it just needs the values that exist right now.</para>
    /// </summary>
    Task<List<GoLiveLookupListDto>> GetLookupOptions();
}
