using System.Threading.Tasks;
using Abp.Application.Services.Dto;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

public class ProxyMyGeoPunchAppService : ProxyAppServiceBase, IMyGeoPunchAppService
{
    public async Task<MyGeoPunchContextDto> GetMyGeoPunchContext()
    {
        return await ApiClient.GetAsync<MyGeoPunchContextDto>(GetEndpoint(nameof(GetMyGeoPunchContext)));
    }

    public async Task<MyGeoPunchLocationCheckDto> CheckMyGeoPunchLocation(CheckMyGeoPunchLocationInput input)
    {
        return await ApiClient.PostAsync<MyGeoPunchLocationCheckDto>(GetEndpoint(nameof(CheckMyGeoPunchLocation)), input);
    }

    public async Task<MyGeoPunchResultDto> RecordMyGeoPunch(RecordMyGeoPunchInput input)
    {
        return await ApiClient.PostAsync<MyGeoPunchResultDto>(GetEndpoint(nameof(RecordMyGeoPunch)), input);
    }

    public async Task<MyGeoPunchSelfieDto> GetMyGeoPunchSelfie(EntityDto input)
    {
        return await ApiClient.GetAsync<MyGeoPunchSelfieDto>(GetEndpoint(nameof(GetMyGeoPunchSelfie)), input);
    }
}
