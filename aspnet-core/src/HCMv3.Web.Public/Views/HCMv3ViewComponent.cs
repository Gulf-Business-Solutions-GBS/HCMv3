using Abp.AspNetCore.Mvc.ViewComponents;

namespace HCMv3.Web.Public.Views;

public abstract class HCMv3ViewComponent : AbpViewComponent
{
    protected HCMv3ViewComponent()
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }
}

