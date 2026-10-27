using Abp;

namespace HCMv3;

/// <summary>
/// This class can be used as a base class for services in this application.
/// It has some useful objects property-injected and has some basic methods most of services may need to.
/// It's suitable for non domain nor application service classes.
/// For domain services inherit <see cref="HCMv3DomainServiceBase"/>.
/// For application services inherit HCMv3AppServiceBase.
/// </summary>
public abstract class HCMv3ServiceBase : AbpServiceBase
{
    protected HCMv3ServiceBase()
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }
}

