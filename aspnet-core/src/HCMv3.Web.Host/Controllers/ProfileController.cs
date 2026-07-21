using Abp.AspNetCore.Mvc.Authorization;
using HCMv3.Authorization.Users.Profile;
using HCMv3.Storage;

namespace HCMv3.Web.Controllers;

[AbpMvcAuthorize]
public class ProfileController : ProfileControllerBase
{
    public ProfileController(
        ITempFileCacheManager tempFileCacheManager,
        IProfileAppService profileAppService) :
        base(tempFileCacheManager, profileAppService)
    {
    }
}

