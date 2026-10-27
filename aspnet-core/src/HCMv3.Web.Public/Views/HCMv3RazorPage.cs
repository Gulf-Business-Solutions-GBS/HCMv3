using Abp.AspNetCore.Mvc.Views;
using Abp.Runtime.Session;
using Microsoft.AspNetCore.Mvc.Razor.Internal;

namespace HCMv3.Web.Public.Views;

public abstract class HCMv3RazorPage<TModel> : AbpRazorPage<TModel>
{
    [RazorInject]
    public IAbpSession AbpSession { get; set; }

    protected HCMv3RazorPage()
    {
        LocalizationSourceName = HCMv3Consts.LocalizationSourceName;
    }
}

