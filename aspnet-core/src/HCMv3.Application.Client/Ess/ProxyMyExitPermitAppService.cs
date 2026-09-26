using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

public class ProxyMyExitPermitAppService : ProxyAppServiceBase, IMyExitPermitAppService
{
    public async Task<PagedResultDto<MyExitPermitDto>> GetMyExitPermits(GetMyRequestsInput input)
    {
        return await ApiClient.PostAsync<PagedResultDto<MyExitPermitDto>>(GetEndpoint(nameof(GetMyExitPermits)), input);
    }

    public async Task<MyRequestSubmissionDto> RequestExitPermit(RequestExitPermitInput input)
    {
        return await ApiClient.PostAsync<MyRequestSubmissionDto>(GetEndpoint(nameof(RequestExitPermit)), input);
    }

    public async Task<MyRequestSubmissionDto> SaveMyExitPermitDraft(SaveExitPermitDraftInput input)
    {
        return await ApiClient.PostAsync<MyRequestSubmissionDto>(GetEndpoint(nameof(SaveMyExitPermitDraft)), input);
    }

    public async Task<MyExitPermitDto> GetMyExitPermitDraftForEdit(EntityDto input)
    {
        return await ApiClient.GetAsync<MyExitPermitDto>(GetEndpoint(nameof(GetMyExitPermitDraftForEdit)), input);
    }

    public async Task<MyRequestSubmissionDto> SubmitMyExitPermitDraft(EntityDto input)
    {
        return await ApiClient.PostAsync<MyRequestSubmissionDto>(GetEndpoint(nameof(SubmitMyExitPermitDraft)), input);
    }

    public async Task CancelMyExitPermit(EntityDto input)
    {
        await ApiClient.PostAsync(GetEndpoint(nameof(CancelMyExitPermit)), input);
    }
}
