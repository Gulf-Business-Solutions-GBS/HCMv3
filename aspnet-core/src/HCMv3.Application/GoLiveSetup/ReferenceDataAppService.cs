using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Extensions;
using Abp.Runtime.Session;
using HCMv3.Authorization;
using HCMv3.GoLiveSetup.Dto;
using HCMv3.GoLiveSetup.ReferenceData;

namespace HCMv3.GoLiveSetup;

/// <inheritdoc cref="IReferenceDataAppService"/>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class ReferenceDataAppService : HCMv3AppServiceBase, IReferenceDataAppService
{
    private readonly IReferenceDataSeeder _seeder;

    public ReferenceDataAppService(IReferenceDataSeeder seeder)
    {
        _seeder = seeder;
    }

    public async Task<List<ReferenceDataPackDto>> GetPacks()
    {
        var packs = new List<ReferenceDataPackDto>();

        foreach (var packId in _seeder.GetAvailablePackIds())
        {
            var pack = _seeder.GetPack(packId);

            // The preview walks the same code path the seed does, so the numbers shown here cannot drift
            // from what pressing the button actually produces.
            var preview = await _seeder.PreviewPackAsync(packId);

            packs.Add(new ReferenceDataPackDto
            {
                Id = pack.Id,
                NameEn = pack.NameEn,
                NameAr = pack.NameAr,
                Description = pack.Description,
                Lines = preview.Counts
                    .Select(c => new ReferenceDataPackLineDto
                    {
                        Lookup = c.Lookup,
                        Available = c.Inserted + c.Skipped,
                        AlreadyPresent = c.Skipped,
                    })
                    .ToList(),
                TotalRows = preview.TotalInserted + preview.TotalSkipped,
            });
        }

        return packs;
    }

    public async Task<SeedReferenceDataOutput> SeedReferenceData(SeedReferenceDataInput input)
    {
        var tenantId = AbpSession.GetTenantId();

        var results = input.PackId.IsNullOrWhiteSpace()
            ? await _seeder.SeedDefaultsAsync(tenantId)
            : new List<ReferenceDataSeedResult> { await _seeder.SeedPackAsync(input.PackId, tenantId) };

        var output = new SeedReferenceDataOutput
        {
            Packs = results
                .Select(r => new SeedReferenceDataPackResultDto
                {
                    PackId = r.PackId,
                    Inserted = r.TotalInserted,
                    Skipped = r.TotalSkipped,
                    Lines = r.Counts
                        .Select(c => new ReferenceDataPackLineDto
                        {
                            Lookup = c.Lookup,
                            Available = c.Inserted + c.Skipped,
                            AlreadyPresent = c.Skipped,
                        })
                        .ToList(),
                })
                .ToList(),
        };

        output.TotalInserted = output.Packs.Sum(p => p.Inserted);
        output.TotalSkipped = output.Packs.Sum(p => p.Skipped);

        return output;
    }
}
