using Abp.Dependency;

namespace HCMv3.Maui.Services.UI;

public class LanguageService : ISingletonDependency
{
    public event EventHandler OnLanguageChanged;

    public void ChangeLanguage()
    {
        OnLanguageChanged?.Invoke(this, EventArgs.Empty);
    }
}