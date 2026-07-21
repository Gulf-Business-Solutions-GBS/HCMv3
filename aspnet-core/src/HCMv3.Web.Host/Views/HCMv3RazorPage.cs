using Abp.AspNetCore.Mvc.Views;

namespace HCMv3.Web.Views;

public abstract class HCMv3RazorPage<TModel> : AbpRazorPage<TModel>
{
    protected HCMv3RazorPage()
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }
}

