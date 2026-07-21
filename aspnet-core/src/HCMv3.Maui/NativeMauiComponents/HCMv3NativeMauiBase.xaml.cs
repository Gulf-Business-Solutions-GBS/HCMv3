using Abp;
using HCMv3.Maui.Core;

namespace HCMv3.Maui.NativeMauiComponents;

public partial class HCMv3NativeMauiComponentBase : ContentPage
{
    public HCMv3NativeMauiComponentBase()
    {
        InitializeComponent();
    }

    protected static T Resolve<T>()
    {
        return DependencyResolver.Resolve<T>();
    }

    protected string L(string text)
    {
        return Core.Localization.L.Localize(text);
    }

    protected Page GetMainPage()
    {
        var mainPage = Application.Current?.Windows[0].Page;

        if (mainPage is null)
        {
            throw new AbpException("Main page is not set yet.");
        }

        return mainPage;
    }
}