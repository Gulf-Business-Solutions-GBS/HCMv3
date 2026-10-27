using Abp.ObjectMapping;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using HCMv3.Maui.Services.UI;

namespace HCMv3.Maui.Core.Components;

public abstract class HCMv3ComponentBase : ComponentBase
{
    [Inject] protected IJSRuntime JS { get; set; }

    protected UserDialogsService UserDialogsService { get; } = Resolve<UserDialogsService>();

    protected IObjectMapper ObjectMapper { get; } = Resolve<IObjectMapper>();

    protected static T Resolve<T>()
    {
        return DependencyResolver.Resolve<T>();
    }

    protected async Task SetBusyAsync(Func<Task> func)
    {
        await UserDialogsService.Block();
        try
        {
            await func();
        }
        finally
        {
            await UserDialogsService.UnBlock();
        }
    }

    public string L(string text)
    {
        return Core.Localization.L.Localize(text);
    }

    public static string L(string text, params object[] args)
    {
        return Core.Localization.L.Localize(text, args);
    }

    public static string LocalizeWithThreeDots(string text, params object[] args)
    {
        return Core.Localization.L.LocalizeWithThreeDots(text, args);
    }

    public static string LocalizeWithParantheses(string text, object valueWithinParentheses, params object[] args)
    {
        return Core.Localization.L.LocalizeWithParantheses(text, valueWithinParentheses, args);
    }
}