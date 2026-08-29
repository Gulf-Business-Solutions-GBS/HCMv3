using System.Linq;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.Domain.Repositories;
using Abp.Extensions;
using Abp.Runtime.Session;
using HCMv3.Authorization;
using HCMv3.GoLiveSetup.Dto;

namespace HCMv3.GoLiveSetup;

/// <inheritdoc cref="IGoLiveStateAppService"/>
[AbpAuthorize(AppPermissions.Pages_GoLiveSetup)]
public class GoLiveStateAppService : HCMv3AppServiceBase, IGoLiveStateAppService
{
    private readonly IRepository<GoLiveStepState> _states;

    public GoLiveStateAppService(IRepository<GoLiveStepState> states)
    {
        _states = states;
    }

    public async Task<GetGoLiveStateOutput> GetState()
    {
        var rows = await _states.GetAllListAsync();

        var output = new GetGoLiveStateOutput
        {
            Steps = rows
                .OrderBy(r => r.StepId)
                .Select(r => new GoLiveStepStateDto
                {
                    StepId = r.StepId,
                    Payload = r.Payload,
                    LastSavedUtc = r.LastModificationTime ?? r.CreationTime,
                })
                .ToList(),
        };

        output.LastSavedUtc = output.Steps.Any() ? output.Steps.Max(s => s.LastSavedUtc) : null;

        return output;
    }

    public async Task SaveStep(SaveGoLiveStepInput input)
    {
        var existing = await _states.FirstOrDefaultAsync(s => s.StepId == input.StepId);

        // An empty payload means the step was cleared. Deleting rather than storing "{}" keeps GetState
        // honest about which steps have actually been touched.
        if (input.Payload.IsNullOrWhiteSpace())
        {
            if (existing != null)
            {
                await _states.DeleteAsync(existing);
            }

            return;
        }

        if (existing == null)
        {
            await _states.InsertAsync(new GoLiveStepState
            {
                TenantId = AbpSession.GetTenantId(),
                StepId = input.StepId,
                Payload = input.Payload,
            });

            return;
        }

        existing.Payload = input.Payload;
        await _states.UpdateAsync(existing);
    }

    public async Task ClearState()
    {
        await _states.DeleteAsync(s => s.TenantId == AbpSession.GetTenantId());
    }
}
