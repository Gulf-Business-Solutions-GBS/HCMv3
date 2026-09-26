using System.Threading.Tasks;
using HCMv3.Ess.Dto;

namespace HCMv3.Ess;

public class ProxyEssSessionAppService : ProxyAppServiceBase, IEssSessionAppService
{
    public async Task<EssContextDto> GetEssContext()
    {
        return await ApiClient.GetAsync<EssContextDto>(GetEndpoint(nameof(GetEssContext)));
    }
}
