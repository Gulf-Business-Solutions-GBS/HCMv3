using System.Threading.Tasks;
using Abp.Dependency;

namespace HCMv3.UiCustomization;

public interface IUiThemeCustomizerFactory : ISingletonDependency
{
    Task<IUiCustomizer> GetCurrentUiCustomizer();

    IUiCustomizer GetUiCustomizer(string theme);
}
