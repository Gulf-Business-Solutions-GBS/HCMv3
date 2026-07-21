using System.Threading.Tasks;
using Abp.Application.Services;
using Abp.Application.Services.Dto;
using HCMv3.Caching.Dto;

namespace HCMv3.Caching;

public interface ICachingAppService : IApplicationService
{
    ListResultDto<CacheDto> GetAllCaches();

    Task ClearCache(EntityDto<string> input);

    Task ClearAllCaches();

    bool CanClearAllCaches();
}

