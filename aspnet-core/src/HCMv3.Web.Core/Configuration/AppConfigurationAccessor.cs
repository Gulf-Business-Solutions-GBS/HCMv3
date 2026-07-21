using Abp.Dependency;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using HCMv3.Configuration;

namespace HCMv3.Web.Configuration;

public class AppConfigurationAccessor : IAppConfigurationAccessor, ISingletonDependency
{
    public IConfigurationRoot Configuration { get; }

    public AppConfigurationAccessor(IWebHostEnvironment env)
    {
        Configuration = env.GetAppConfiguration();
    }
}

