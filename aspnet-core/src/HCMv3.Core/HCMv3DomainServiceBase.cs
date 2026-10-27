using Abp.Domain.Services;

namespace HCMv3;

public abstract class HCMv3DomainServiceBase : DomainService
{
    /* Add your common members for all your domain services. */

    protected HCMv3DomainServiceBase()
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }
}

