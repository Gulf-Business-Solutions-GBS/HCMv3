using DevExpress.DataAccess.Web;
using DevExpress.DataAccess.Wizard.Services;
using HCMv3.Configuration;

namespace HCMv3.Web.Reporting;

/// <summary>
/// Hands DevExpress's reporting pipeline an <see cref="HcmReportConnectionProviderService"/>.
///
/// WHY THIS EXISTS AT ALL: registering <see cref="IConnectionProviderService"/> in DI is NOT enough.
/// The Web Document Viewer / Report Designer stack resolves
/// <see cref="IConnectionProviderFactory"/> (namespace DevExpress.DataAccess.Web) and calls
/// <c>Create()</c> to obtain the service (namespace DevExpress.DataAccess.Wizard.Services) -- two
/// different types in two different namespaces. With only the service registered, nothing ever
/// consults it and the report layout's connection NAME is never resolved, so
/// <c>SqlDataSource.CreateConnection(name)</c> throws a NullReferenceException the first time the
/// viewer touches the data source -- in practice while populating a parameter's dynamic look-up
/// list, before a single page renders.
///
/// This factory is a singleton because DevExpress resolves it outside any request scope; it builds
/// the service per call from the singleton configuration accessor rather than capturing one.
/// </summary>
public class HcmReportConnectionProviderFactory : IConnectionProviderFactory
{
    private readonly IAppConfigurationAccessor _configurationAccessor;

    public HcmReportConnectionProviderFactory(IAppConfigurationAccessor configurationAccessor)
    {
        _configurationAccessor = configurationAccessor;
    }

    public IConnectionProviderService Create()
    {
        return new HcmReportConnectionProviderService(_configurationAccessor);
    }
}
